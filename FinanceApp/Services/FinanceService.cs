using FinanceApp.Data;
using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Services;

public class FinanceService : IFinanceService
{
    // The monthly rows of outcomes paid with a credit card, the only ones that use up a card's limit.
    private const string CreditCardMonthlyEntriesSql =
        $"SELECT m.* FROM {nameof(FinancialMonthlyEntry)} m " +
        $"JOIN {nameof(FinancialEntry)} e ON e.{nameof(FinancialEntry.Id)} = m.{nameof(FinancialMonthlyEntry.FinancialEntryId)} " +
        $"JOIN {nameof(PaymentSource)} s ON s.{nameof(PaymentSource.Id)} = e.{nameof(FinancialEntry.PaymentSourceId)} " +
        $"WHERE e.{nameof(FinancialEntry.EntryType)} = ? AND s.{nameof(PaymentSource.Type)} = ?";

    private readonly IDatabaseContext _context;
    private readonly ICategoryService _categoryService;
    private readonly IPaymentSourceService _paymentSourceService;

    public FinanceService(IDatabaseContext context, ICategoryService categoryService, IPaymentSourceService paymentSourceService)
    {
        _context = context;
        _categoryService = categoryService;
        _paymentSourceService = paymentSourceService;
    }

    public async Task<IEnumerable<MonthlyEntryProjection>> GetMergedEntriesForMonthAsync(int year, int month)
    {
        var db = await _context.GetConnectionAsync();
        var targetDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);

        var allTemplates = await db.Table<FinancialEntry>().ToListAsync();
        var activeTemplates = allTemplates.Where(e => IsActiveForMonth(e, targetDate)).ToList();

        var physicalRecords = await db.Table<FinancialMonthlyEntry>()
            .Where(r => r.Year == year && r.Month == month)
            .ToListAsync();

        var physicalLookup = physicalRecords.ToDictionary(r => r.FinancialEntryId);

        var categories = await _categoryService.GetAllAsync();
        var categoryLookup = categories.ToDictionary(c => c.Id);

        var sources = await _paymentSourceService.GetAllAsync();
        var sourceLookup = sources.ToDictionary(s => s.Id);

        var projections = new List<MonthlyEntryProjection>();

        foreach (var template in activeTemplates)
        {
            var category = template.CategoryId is { } categoryId ? categoryLookup.GetValueOrDefault(categoryId) : null;
            var source = template.PaymentSourceId is { } sourceId ? sourceLookup.GetValueOrDefault(sourceId) : null;
            var isOnCardBill = template.EntryType == FinancialEntryType.Outcome && source?.Type == PaymentSourceType.CreditCard;
            // An outcome paid with a credit card falls due with the card's bill.
            var dueDate = BuildDueDate(isOnCardBill ? source!.BillDueDay : template.DueDay, year, month);
            var billAccount = isOnCardBill && source!.BillPaymentSourceId is { } accountId ? sourceLookup.GetValueOrDefault(accountId) : null;

            if (physicalLookup.TryGetValue(template.Id, out var record))
            {
                if (record.IsCancelled)
                    continue;

                var (plannedAmount, effectiveAmount, paidAmount, status) = GetMonthAmounts(template, record);

                projections.Add(new MonthlyEntryProjection(
                    FinancialEntryId: template.Id,
                    FinancialMonthlyEntryId: record.Id,
                    Description: template.Description,
                    EffectiveAmount: effectiveAmount,
                    EntryType: template.EntryType,
                    Category: category,
                    Status: status,
                    IsProjected: false,
                    InstallmentLabel: BuildInstallmentLabel(template, year, month),
                    InstallmentTotalAmount: BuildInstallmentTotal(template),
                    PaidAmount: paidAmount,
                    IsEstimated: template.IsEstimated,
                    PlannedAmount: plannedAmount,
                    AllowsPartialPayment: template.AllowsPartialPayment,
                    DueDate: dueDate,
                    PaymentSource: source,
                    BillPaymentAccount: billAccount
                ));
            }
            else
            {
                projections.Add(new MonthlyEntryProjection(
                    FinancialEntryId: template.Id,
                    FinancialMonthlyEntryId: null,
                    Description: template.Description,
                    EffectiveAmount: template.Amount,
                    EntryType: template.EntryType,
                    Category: category,
                    Status: FinancialMonthlyEntryStatus.Pending,
                    IsProjected: true,
                    InstallmentLabel: BuildInstallmentLabel(template, year, month),
                    InstallmentTotalAmount: BuildInstallmentTotal(template),
                    IsEstimated: template.IsEstimated,
                    AllowsPartialPayment: template.AllowsPartialPayment,
                    DueDate: dueDate,
                    PaymentSource: source,
                    BillPaymentAccount: billAccount
                ));
            }
        }

        return projections;
    }

    public async Task<FinancialEntry?> GetEntryByIdAsync(int id)
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveEntryAsync(FinancialEntry entry)
    {
        var db = await _context.GetConnectionAsync();

        if (entry.Id != 0)
            await db.UpdateAsync(entry);
        else
            await db.InsertAsync(entry);
    }

    public async Task SaveMonthlyEntryAsync(FinancialMonthlyEntry entry)
    {
        var db = await _context.GetConnectionAsync();

        if (entry.Id != 0)
            await db.UpdateAsync(entry);
        else
            await db.InsertAsync(entry);
    }

    public Task SetEntryStatusAsync(int entryId, int year, int month, FinancialMonthlyEntryStatus status) =>
        InTransactionAsync(conn => SetEntryStatus(conn, entryId, year, month, status));

    public async Task SetCreditCardBillStatusAsync(int cardId, int year, int month, FinancialMonthlyEntryStatus status)
    {
        var entries = await GetMergedEntriesForMonthAsync(year, month);
        var billEntryIds = entries
            .Where(e => e.IsOnCreditCardBill && e.PaymentSource!.Id == cardId && e.Status != status)
            .Select(e => e.FinancialEntryId)
            .ToList();

        await InTransactionAsync(conn =>
        {
            foreach (var entryId in billEntryIds)
                SetEntryStatus(conn, entryId, year, month, status);
        });
    }

    private static void SetEntryStatus(SQLiteConnection conn, int entryId, int year, int month, FinancialMonthlyEntryStatus status)
    {
        var template = conn.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefault();
        var record = GetOrCreateMonthlyRecord(conn, entryId, year, month);
        var plannedAmount = record.OverrideAmount ?? template?.Amount;
        record.Status = status;
        // Paying settles the whole planned amount, unless more than that was already paid (e.g. a bill that came higher).
        // Reverting to pending resets the month.
        record.PaidAmount = status == FinancialMonthlyEntryStatus.Paid && record.PaidAmount > plannedAmount
            ? record.PaidAmount
            : null;

        Upsert(conn, record);

        // Reverting to pending resets the month, while paying the rest keeps the history of what was paid so far.
        if (status == FinancialMonthlyEntryStatus.Pending)
            DeletePayments(conn, record.Id);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetUsedCreditLimitsAsync(int year, int month)
    {
        var db = await _context.GetConnectionAsync();
        var currentMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);

        var sources = await _paymentSourceService.GetAllAsync();
        var usedLimits = sources.Where(s => s.Type == PaymentSourceType.CreditCard).ToDictionary(s => s.Id, _ => 0m);
        if (usedLimits.Count == 0)
            return usedLimits;

        var templates = await db.Table<FinancialEntry>()
            .Where(e => e.EntryType == FinancialEntryType.Outcome && e.PaymentSourceId != null)
            .ToListAsync();
        var cardTemplates = templates.Where(e => usedLimits.ContainsKey(e.PaymentSourceId!.Value)).ToList();

        var records = await db.QueryAsync<FinancialMonthlyEntry>(
            CreditCardMonthlyEntriesSql, (int)FinancialEntryType.Outcome, (int)PaymentSourceType.CreditCard);
        var recordLookup = records.ToDictionary(r => (r.FinancialEntryId, r.Year, r.Month));

        foreach (var template in cardTemplates)
        {
            foreach (var target in GetMonthsUsingCreditLimit(template, currentMonth))
            {
                var record = recordLookup.GetValueOrDefault((template.Id, target.Year, target.Month));
                if (record?.IsCancelled == true)
                    continue;

                var (_, effectiveAmount, paidAmount, _) = GetMonthAmounts(template, record);
                usedLimits[template.PaymentSourceId!.Value] += effectiveAmount - paidAmount;
            }
        }

        return usedLimits;
    }

    // A card outcome takes up the limit in each of its months up to the current one; a purchase in installments,
    // in all of its months, since the card holds its whole remaining amount from the start.
    private static IEnumerable<DateTime> GetMonthsUsingCreditLimit(FinancialEntry entry, DateTime currentMonth)
    {
        var firstOfStart = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var lastMonth = entry.Recurrence == RecurrenceType.Installments
            ? firstOfStart.AddMonths((entry.TotalInstallments ?? 0) - 1)
            : currentMonth;

        for (var target = firstOfStart; target <= lastMonth; target = target.AddMonths(1))
            if (IsActiveForMonth(entry, target))
                yield return target;
    }

    // The month's amounts: what was planned, the actual amount (paying more than planned, e.g. an estimated bill
    // that came higher, raises it) and what was paid so far.
    private static (decimal Planned, decimal Effective, decimal Paid, FinancialMonthlyEntryStatus Status) GetMonthAmounts(
        FinancialEntry template, FinancialMonthlyEntry? record)
    {
        if (record is null)
            return (template.Amount, template.Amount, 0, FinancialMonthlyEntryStatus.Pending);

        var plannedAmount = record.OverrideAmount ?? template.Amount;
        var status = GetEffectiveStatus(record);
        var effectiveAmount = Math.Max(plannedAmount, record.PaidAmount ?? 0);
        var paidAmount = status switch
        {
            FinancialMonthlyEntryStatus.Paid => effectiveAmount,
            FinancialMonthlyEntryStatus.PartiallyPaid => record.PaidAmount ?? 0,
            _ => 0
        };

        return (plannedAmount, effectiveAmount, paidAmount, status);
    }

    public Task SetPaidAmountAsync(int entryId, int year, int month, decimal paidAmount) =>
        // Typing a total below the planned amount reopens a settled month.
        UpdatePaidAmountAsync(entryId, year, month, (_, _) => paidAmount, reopensBelowPlanned: true);

    public Task AddPaymentAsync(int entryId, int year, int month, decimal amount, DateTime date, string description) =>
        UpdatePaidAmountAsync(entryId, year, month,
            (record, plannedAmount) => GetAlreadyPaid(record, plannedAmount) + amount,
            (conn, record) => conn.Insert(new FinancialPayment
            {
                FinancialMonthlyEntryId = record.Id,
                Amount = amount,
                Date = date.Date,
                Description = description
            }));

    public async Task<IReadOnlyList<FinancialPayment>> GetPaymentsAsync(int entryId, int year, int month)
    {
        var db = await _context.GetConnectionAsync();

        var record = await db.Table<FinancialMonthlyEntry>()
            .Where(r => r.FinancialEntryId == entryId && r.Year == year && r.Month == month)
            .FirstOrDefaultAsync();
        if (record is null)
            return [];

        var payments = await db.Table<FinancialPayment>()
            .Where(p => p.FinancialMonthlyEntryId == record.Id)
            .ToListAsync();

        return [.. payments.OrderBy(p => p.Date).ThenBy(p => p.Id)];
    }

    public Task DeletePaymentAsync(int paymentId) =>
        InTransactionAsync(conn =>
        {
            var payment = conn.Table<FinancialPayment>().Where(p => p.Id == paymentId).FirstOrDefault();
            if (payment is null)
                return;

            conn.Delete(payment);

            var record = conn.Table<FinancialMonthlyEntry>()
                .Where(r => r.Id == payment.FinancialMonthlyEntryId)
                .FirstOrDefault();
            if (record is not null)
                UpdatePaidAmount(conn, record.FinancialEntryId, record.Year, record.Month,
                    (r, plannedAmount) => GetAlreadyPaid(r, plannedAmount) - payment.Amount);
        });

    private Task UpdatePaidAmountAsync(
        int entryId, int year, int month,
        Func<FinancialMonthlyEntry, decimal, decimal> newPaidAmount,
        Action<SQLiteConnection, FinancialMonthlyEntry>? afterSave = null,
        bool reopensBelowPlanned = false) =>
        InTransactionAsync(conn => UpdatePaidAmount(conn, entryId, year, month, newPaidAmount, afterSave, reopensBelowPlanned));

    private static void UpdatePaidAmount(
        SQLiteConnection conn,
        int entryId, int year, int month,
        Func<FinancialMonthlyEntry, decimal, decimal> newPaidAmount,
        Action<SQLiteConnection, FinancialMonthlyEntry>? afterSave = null,
        bool reopensBelowPlanned = false)
    {
        var template = conn.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefault();
        if (template is null)
            return;

        var record = GetOrCreateMonthlyRecord(conn, entryId, year, month);
        var plannedAmount = record.OverrideAmount ?? template.Amount;
        var paidAmount = newPaidAmount(record, plannedAmount);
        var isCleared = paidAmount <= 0;

        if (isCleared)
        {
            record.PaidAmount = null;
            record.Status = FinancialMonthlyEntryStatus.Pending;
        }
        else
        {
            if (reopensBelowPlanned && paidAmount < plannedAmount)
                record.Status = FinancialMonthlyEntryStatus.PartiallyPaid;

            // A settled month within its planned amount is just "the whole amount paid".
            record.PaidAmount = record.Status == FinancialMonthlyEntryStatus.Paid && paidAmount <= plannedAmount
                ? null
                : paidAmount;
            record.Status = GetEffectiveStatus(record);
        }

        Upsert(conn, record);

        if (isCleared)
            DeletePayments(conn, record.Id);
        else
            afterSave?.Invoke(conn, record);
    }

    // An entry marked as paid without partial payments counts as having its whole amount paid.
    private static decimal GetAlreadyPaid(FinancialMonthlyEntry record, decimal plannedAmount) =>
        record.PaidAmount ?? (record.Status == FinancialMonthlyEntryStatus.Paid ? plannedAmount : 0);

    public Task DeleteEntryAsync(int entryId, int year, int month, EntryDeleteScope scope) =>
        InTransactionAsync(conn =>
        {
            var template = conn.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefault();
            if (template is null)
                return;

            var targetDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
            var firstOfStart = new DateTime(template.StartDate.Year, template.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);

            if (template.Recurrence == RecurrenceType.OneTime ||
                (scope == EntryDeleteScope.ThisAndFuture && targetDate <= firstOfStart))
                scope = EntryDeleteScope.All;

            var monthlyRecords = conn.Table<FinancialMonthlyEntry>()
                .Where(r => r.FinancialEntryId == entryId)
                .ToList();

            switch (scope)
            {
                case EntryDeleteScope.ThisMonth:
                    var record = monthlyRecords.FirstOrDefault(r => r.Year == year && r.Month == month)
                        ?? new FinancialMonthlyEntry { FinancialEntryId = entryId, Year = year, Month = month };
                    record.IsCancelled = true;
                    Upsert(conn, record);
                    break;

                case EntryDeleteScope.ThisAndFuture:
                    template.EndDate = targetDate.AddMonths(-1);
                    conn.Update(template);

                    foreach (var r in monthlyRecords.Where(r => r.Year * 12 + r.Month >= year * 12 + month))
                        DeleteMonthlyRecord(conn, r);
                    break;

                case EntryDeleteScope.All:
                    foreach (var r in monthlyRecords)
                        DeleteMonthlyRecord(conn, r);

                    conn.Delete(template);
                    break;
            }
        });

    public Task ClearAllDataAsync() =>
        InTransactionAsync(conn =>
        {
            conn.DeleteAll<FinancialEntry>();
            conn.DeleteAll<FinancialMonthlyEntry>();
            conn.DeleteAll<FinancialPayment>();
        });

    // Writes touching more than one row run in a transaction, so an app killed halfway leaves no half-made change.
    private async Task InTransactionAsync(Action<SQLiteConnection> write)
    {
        var db = await _context.GetConnectionAsync();
        await db.RunInTransactionAsync(write);
    }

    // Only an explicit "Pagar" settles the month: payments that reach or pass the planned amount keep it partially paid,
    // since an estimated expense (e.g. supermarket) can keep growing until the month ends.
    private static FinancialMonthlyEntryStatus GetEffectiveStatus(FinancialMonthlyEntry record) =>
        record switch
        {
            { Status: FinancialMonthlyEntryStatus.Paid } => FinancialMonthlyEntryStatus.Paid,
            { PaidAmount: > 0 } => FinancialMonthlyEntryStatus.PartiallyPaid,
            _ => FinancialMonthlyEntryStatus.Pending
        };

    private static FinancialMonthlyEntry GetOrCreateMonthlyRecord(SQLiteConnection conn, int entryId, int year, int month) =>
        conn.Table<FinancialMonthlyEntry>()
            .Where(r => r.FinancialEntryId == entryId && r.Year == year && r.Month == month)
            .FirstOrDefault()
        ?? new FinancialMonthlyEntry { FinancialEntryId = entryId, Year = year, Month = month };

    private static void Upsert(SQLiteConnection conn, FinancialMonthlyEntry record)
    {
        if (record.Id != 0)
            conn.Update(record);
        else
            conn.Insert(record);
    }

    private static void DeleteMonthlyRecord(SQLiteConnection conn, FinancialMonthlyEntry record)
    {
        DeletePayments(conn, record.Id);
        conn.Delete(record);
    }

    private static void DeletePayments(SQLiteConnection conn, int monthlyEntryId) =>
        conn.Table<FinancialPayment>().Delete(p => p.FinancialMonthlyEntryId == monthlyEntryId);

    private static bool IsActiveForMonth(FinancialEntry entry, DateTime targetDate)
    {
        var firstOfStart = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);

        if (entry.EndDate is { } end &&
            targetDate > new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Local))
            return false;

        return entry.Recurrence switch
        {
            RecurrenceType.OneTime =>
                entry.StartDate.Year == targetDate.Year && entry.StartDate.Month == targetDate.Month,

            RecurrenceType.Installments =>
                targetDate >= firstOfStart &&
                targetDate < firstOfStart.AddMonths(entry.TotalInstallments ?? 0),

            RecurrenceType.Recurrent =>
                firstOfStart <= targetDate,

            RecurrenceType.Annual =>
                MonthsSinceStart(entry, targetDate.Year, targetDate.Month) is var months and >= 0
                && months % 12 < AnnualInstallments(entry),

            _ => false
        };
    }

    private static string? BuildInstallmentLabel(FinancialEntry entry, int year, int month) => entry.Recurrence switch
    {
        RecurrenceType.Installments => $"{MonthsSinceStart(entry, year, month) + 1}/{entry.TotalInstallments}",
        RecurrenceType.Annual when AnnualInstallments(entry) > 1 =>
            $"{MonthsSinceStart(entry, year, month) % 12 + 1}/{AnnualInstallments(entry)}",
        _ => null
    };

    private static int MonthsSinceStart(FinancialEntry entry, int year, int month) =>
        (year - entry.StartDate.Year) * 12 + (month - entry.StartDate.Month);

    // An annual entry is paid at once (one installment) or in up to 12 consecutive months each year.
    private static int AnnualInstallments(FinancialEntry entry) => Math.Clamp(entry.TotalInstallments ?? 1, 1, 12);

    private static DateTime? BuildDueDate(int? dueDay, int year, int month) =>
        dueDay is { } day
            ? new DateTime(year, month, Math.Clamp(day, 1, DateTime.DaysInMonth(year, month)), 0, 0, 0, DateTimeKind.Local)
            : null;

    // For an annual entry in installments, the total of each year.
    private static decimal? BuildInstallmentTotal(FinancialEntry entry) => entry.Recurrence switch
    {
        RecurrenceType.Installments => entry.Amount * entry.TotalInstallments,
        RecurrenceType.Annual when AnnualInstallments(entry) > 1 => entry.Amount * AnnualInstallments(entry),
        _ => null
    };
}
