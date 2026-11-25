using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class SetupService : ISetupService
{
    private const string CompletedKey = "SetupCompleted";

    private readonly IDatabaseContext _context;
    private readonly ICategoryService _categoryService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IPreferencesService _preferences;
    private readonly TimeProvider _timeProvider;

    public SetupService(
        IDatabaseContext context,
        ICategoryService categoryService,
        IPaymentSourceService paymentSourceService,
        IUserProfileService profileService,
        IPreferencesService preferences,
        TimeProvider timeProvider)
    {
        _context = context;
        _categoryService = categoryService;
        _paymentSourceService = paymentSourceService;
        _profileService = profileService;
        _preferences = preferences;
        _timeProvider = timeProvider;
    }

    public async Task<bool> ShouldShowAsync()
    {
        if (_preferences.Get(CompletedKey, false))
            return false;

        var db = await _context.GetConnectionAsync();
        var hasData = await db.Table<FinancialEntry>().CountAsync() > 0
            || await db.Table<PaymentSource>().CountAsync() > 0;
        if (hasData)
            Skip();

        return !hasData;
    }

    public async Task CompleteAsync(NewUserSetup setup)
    {
        var categories = await _categoryService.GetAllAsync();
        var now = _timeProvider.GetLocalNow();
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);

        // All or nothing: a wizard that fails halfway leaves no accounts or entries behind, so it can simply be run again.
        var db = await _context.GetConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            foreach (var source in setup.PaymentSources)
            {
                PaymentSourceService.Normalize(source);
                conn.Insert(source);
            }

            foreach (var item in setup.CreditCards)
            {
                item.Card.Type = PaymentSourceType.CreditCard;
                item.Card.BillPaymentSourceId = item.BillAccount?.Id;
                PaymentSourceService.Normalize(item.Card);
                conn.Insert(item.Card);
            }

            foreach (var item in setup.Entries.Where(e => e.Amount > 0))
            {
                var entry = BuildEntry(item, startDate);
                var stored = item.ExistingEntryId is { } id ? conn.Find<FinancialEntry>(id) : null;
                if (stored is not null && KeepsSchedule(stored, entry))
                {
                    Update(conn, stored, entry, item, now);
                    conn.Table<SetupLink>().Delete(l => l.FinancialEntryId == stored.Id);
                    entry = stored;
                }
                else if (stored is not null)
                {
                    // A new schedule (monthly to yearly, other months, other installments) would move the entry's past
                    // months, so the old entry ends last month, keeping what was paid, and a new one starts.
                    var categoryIds = conn.Table<FinancialCategoryEntry>().Where(l => l.FinancialEntryId == stored.Id)
                        .ToList().OrderBy(l => l.Position).Select(l => l.FinancialCategoryId).ToList();
                    if (item.Row is not (SetupRows.Purchase or SetupRows.Activity))
                        entry.Description = stored.Description;
                    FinanceService.DeleteEntry(conn, stored.Id, now.Year, now.Month, EntryDeleteScope.ThisAndFuture);
                    conn.Table<SetupLink>().Delete(l => l.FinancialEntryId == stored.Id);
                    conn.Insert(entry);
                    FinanceService.SetCategories(conn, entry.Id, categoryIds);

                    // Installments counted from the past (e.g. the 50th of 120 this month) would repeat the months the
                    // old entry already had, so the new one starts showing from this month in those.
                    var oldStart = new DateTime(stored.StartDate.Year, stored.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);
                    for (var past = entry.StartDate < oldStart ? oldStart : entry.StartDate; past < startDate; past = past.AddMonths(1))
                        conn.Insert(new FinancialMonthlyEntry { FinancialEntryId = entry.Id, Year = past.Year, Month = past.Month, IsCancelled = true });
                }
                else
                {
                    conn.Insert(entry);
                    var category = categories.FirstOrDefault(c => c.EntryType == item.EntryType && c.Name == item.CategoryName);
                    if (category is not null)
                        FinanceService.SetCategories(conn, entry.Id, [category.Id]);
                }

                conn.Insert(new SetupLink { Row = item.Row, Group = item.Group, GroupName = item.GroupName, FinancialEntryId = entry.Id });
            }

            foreach (var entryId in setup.EntriesToRemove)
            {
                FinanceService.DeleteEntry(conn, entryId, now.Year, now.Month, setup.RemoveScope);
                conn.Table<SetupLink>().Delete(l => l.FinancialEntryId == entryId);
            }
        });
        _paymentSourceService.ClearCache();

        await _profileService.SaveAsync(setup.Name, setup.PhotoPath);
        Skip();
    }

    public async Task<IReadOnlyList<PreviousSetupEntry>> GetPreviousSetupAsync()
    {
        var db = await _context.GetConnectionAsync();
        var links = await db.Table<SetupLink>().ToListAsync();
        if (links.Count == 0)
            return [];

        var entries = (await db.Table<FinancialEntry>().ToListAsync()).ToDictionary(e => e.Id);
        var now = _timeProvider.GetLocalNow();
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);

        return
        [
            .. links
                .OrderBy(l => l.Id)
                .Where(l => entries.ContainsKey(l.FinancialEntryId))
                .Select(l => new PreviousSetupEntry(l, entries[l.FinancialEntryId]))
                .Where(p => IsStillRunning(p.Entry, currentMonth))
        ];
    }

    /// <summary>
    /// An entry still to come this month or later: not ended, nor a purchase whose last installment already passed.
    /// Those that ended are history, so the wizard leaves them out (and never removes them).
    /// </summary>
    private static bool IsStillRunning(FinancialEntry entry, DateTime currentMonth)
    {
        if (entry.EndDate is { } end && end < currentMonth)
            return false;

        var start = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);
        return entry.Recurrence switch
        {
            RecurrenceType.OneTime => start >= currentMonth,
            RecurrenceType.Installments => start.AddMonths(Math.Max(entry.TotalInstallments ?? 1, 1) - 1) >= currentMonth,
            _ => true
        };
    }

    /// <summary>
    /// Updates an entry the wizard created before with what the row holds now. A new amount applies from the current
    /// month on, so the past months keep theirs, and the entry keeps its start (and so its past months) while its
    /// schedule is the same. The entry keeps its name, which the user may have changed, unless the wizard row is
    /// where it is typed (a purchase or an activity), and its categories.
    /// </summary>
    private static void Update(SQLite.SQLiteConnection conn, FinancialEntry stored, FinancialEntry entry, SetupEntry item, DateTimeOffset now)
    {
        var amountChanged = stored.Amount != entry.Amount;
        if (item.Row is SetupRows.Purchase or SetupRows.Activity)
            stored.Description = entry.Description;
        stored.Amount = entry.Amount;
        stored.DueDay = entry.DueDay;
        stored.IsEstimated = entry.IsEstimated;
        stored.AllowsPartialPayment = entry.AllowsPartialPayment;
        stored.PaymentSourceId = entry.PaymentSourceId;

        if (amountChanged)
            FinanceService.SaveEntry(conn, stored, null, now.Year, now.Month, AmountChangeScope.ThisAndFuture);
        else
            conn.Update(stored);
    }

    // A recurrent or one-time entry has nothing to recompute; an annual one keeps its start while it falls in the
    // same months, and an installments one while its current installment and total are the same.
    private static bool KeepsSchedule(FinancialEntry stored, FinancialEntry entry) =>
        stored.Recurrence == entry.Recurrence && entry.Recurrence switch
        {
            RecurrenceType.OneTime or RecurrenceType.Recurrent => true,
            RecurrenceType.Annual => stored.StartDate.Month == entry.StartDate.Month && stored.TotalInstallments == entry.TotalInstallments,
            _ => stored.StartDate == entry.StartDate && stored.TotalInstallments == entry.TotalInstallments
        };

    private static FinancialEntry BuildEntry(SetupEntry item, DateTime startDate)
    {
        var (entryStart, totalInstallments) = item.Recurrence switch
        {
            RecurrenceType.Installments => InstallmentsSchedule(item, startDate),
            RecurrenceType.Annual => AnnualSchedule(item, startDate),
            _ => (startDate, (int?)null)
        };
        // A credit card purchase is paid with the bill, so it is never estimated nor paid little by little.
        var isOnCardBill = item.EntryType == FinancialEntryType.Outcome && item.PaymentSource?.Type == PaymentSourceType.CreditCard;

        return new FinancialEntry
        {
            Description = item.Description.Trim(),
            Amount = item.Amount,
            EntryType = item.EntryType,
            Recurrence = item.Recurrence,
            TotalInstallments = totalInstallments,
            StartDate = entryStart,
            DueDay = item.DueDay,
            IsEstimated = item.IsEstimated && !isOnCardBill,
            AllowsPartialPayment = item.AllowsPartialPayment && !isOnCardBill,
            PaymentSourceId = item.PaymentSource?.Id
        };
    }

    public void Skip() => _preferences.Set(CompletedKey, true);

    // The current installment falls in the current month, so the purchase started that many months earlier.
    private static (DateTime Start, int? TotalInstallments) InstallmentsSchedule(SetupEntry item, DateTime currentMonth)
    {
        var total = Math.Max(item.TotalInstallments ?? 1, 1);
        var current = Math.Clamp(item.CurrentInstallment ?? 1, 1, total);
        return (currentMonth.AddMonths(1 - current), total);
    }

    // An annual entry starts in this year's start month while any of this year's installments is still to come
    // (earlier ones show as pending in past months), otherwise in next year's.
    private static (DateTime Start, int? TotalInstallments) AnnualSchedule(SetupEntry item, DateTime currentMonth)
    {
        var installments = Math.Clamp(item.TotalInstallments ?? 1, 1, 12);
        var start = new DateTime(currentMonth.Year, item.AnnualStartMonth ?? currentMonth.Month, 1, 0, 0, 0, DateTimeKind.Local);
        if (start.AddMonths(installments - 1) < currentMonth)
            start = start.AddYears(1);
        return (start, installments > 1 ? installments : null);
    }
}
