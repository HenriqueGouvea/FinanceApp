using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class SetupService : ISetupService
{
    private const string CompletedKey = "SetupCompleted";

    private readonly IDatabaseContext _context;
    private readonly ICategoryService _categoryService;
    private readonly IUserProfileService _profileService;
    private readonly IPreferencesService _preferences;
    private readonly TimeProvider _timeProvider;

    public SetupService(
        IDatabaseContext context,
        ICategoryService categoryService,
        IUserProfileService profileService,
        IPreferencesService preferences,
        TimeProvider timeProvider)
    {
        _context = context;
        _categoryService = categoryService;
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
                conn.Insert(BuildEntry(item, categories, startDate));
        });

        await _profileService.SaveAsync(setup.Name, setup.PhotoPath);
        Skip();
    }

    private static FinancialEntry BuildEntry(SetupEntry item, IReadOnlyList<FinancialCategory> categories, DateTime startDate)
    {
        var category = categories.FirstOrDefault(c => c.EntryType == item.EntryType && c.Name == item.CategoryName);
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
            CategoryId = category?.Id,
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
