using FinanceApp.Data;
using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Services;

public class FinanceService : IFinanceService
{
    private readonly IDatabaseContext _context;

    public FinanceService(IDatabaseContext context)
    {
        _context = context;
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

        var categories = await db.Table<FinancialCategory>().ToListAsync();
        var categoryLookup = categories.ToDictionary(c => c.Id);

        var projections = new List<MonthlyEntryProjection>();

        foreach (var template in activeTemplates)
        {
            var category = template.CategoryId is { } categoryId ? categoryLookup.GetValueOrDefault(categoryId) : null;

            if (physicalLookup.TryGetValue(template.Id, out var record))
            {
                if (record.IsCancelled)
                    continue;

                var plannedAmount = record.OverrideAmount ?? template.Amount;
                var status = GetEffectiveStatus(record);
                // Paying more than planned (e.g. an estimated bill that came higher) raises the month's amount.
                var effectiveAmount = Math.Max(plannedAmount, record.PaidAmount ?? 0);

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
                    PaidAmount: status switch
                    {
                        FinancialMonthlyEntryStatus.Paid => effectiveAmount,
                        FinancialMonthlyEntryStatus.PartiallyPaid => record.PaidAmount ?? 0,
                        _ => 0
                    },
                    IsEstimated: template.IsEstimated,
                    PlannedAmount: plannedAmount
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
                    IsEstimated: template.IsEstimated
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
        try
        {
            var db = await _context.GetConnectionAsync();

            if (entry.Id != 0)
                await db.UpdateAsync(entry);
            else
                await db.InsertAsync(entry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving entry: {ex.Message}");
        }
    }

    public async Task SaveMonthlyEntryAsync(FinancialMonthlyEntry entry)
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            if (entry.Id != 0)
                await db.UpdateAsync(entry);
            else
                await db.InsertAsync(entry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving monthly entry: {ex.Message}");
        }
    }

    public async Task SetEntryStatusAsync(int entryId, int year, int month, FinancialMonthlyEntryStatus status)
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            var template = await db.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefaultAsync();
            var record = await GetOrCreateMonthlyRecordAsync(db, entryId, year, month);
            var plannedAmount = record.OverrideAmount ?? template?.Amount;
            record.Status = status;
            // Paying settles the whole planned amount, unless more than that was already paid (e.g. a bill that came higher).
            // Reverting to pending resets the month.
            record.PaidAmount = status == FinancialMonthlyEntryStatus.Paid && record.PaidAmount > plannedAmount
                ? record.PaidAmount
                : null;

            await UpsertAsync(db, record);

            // Reverting to pending resets the month, while paying the rest keeps the history of what was paid so far.
            if (status == FinancialMonthlyEntryStatus.Pending)
                await DeletePaymentsAsync(db, record.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting entry status: {ex.Message}");
        }
    }

    public Task SetPaidAmountAsync(int entryId, int year, int month, decimal paidAmount) =>
        // Typing a total below the planned amount reopens a settled month.
        UpdatePaidAmountAsync(entryId, year, month, (_, _) => paidAmount, reopensBelowPlanned: true);

    public Task AddPaymentAsync(int entryId, int year, int month, decimal amount, DateTime date, string description) =>
        UpdatePaidAmountAsync(entryId, year, month,
            (record, plannedAmount) => GetAlreadyPaid(record, plannedAmount) + amount,
            (db, record) => db.InsertAsync(new FinancialPayment
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

    public async Task DeletePaymentAsync(int paymentId)
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            var payment = await db.Table<FinancialPayment>().Where(p => p.Id == paymentId).FirstOrDefaultAsync();
            if (payment is null)
                return;

            await db.DeleteAsync(payment);

            var record = await db.Table<FinancialMonthlyEntry>()
                .Where(r => r.Id == payment.FinancialMonthlyEntryId)
                .FirstOrDefaultAsync();
            if (record is not null)
                await UpdatePaidAmountAsync(record.FinancialEntryId, record.Year, record.Month,
                    (r, plannedAmount) => GetAlreadyPaid(r, plannedAmount) - payment.Amount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting payment: {ex.Message}");
        }
    }

    private async Task UpdatePaidAmountAsync(
        int entryId, int year, int month,
        Func<FinancialMonthlyEntry, decimal, decimal> newPaidAmount,
        Func<SQLiteAsyncConnection, FinancialMonthlyEntry, Task>? afterSave = null,
        bool reopensBelowPlanned = false)
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            var template = await db.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefaultAsync();
            if (template is null)
                return;

            var record = await GetOrCreateMonthlyRecordAsync(db, entryId, year, month);
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

            await UpsertAsync(db, record);

            if (isCleared)
                await DeletePaymentsAsync(db, record.Id);
            else if (afterSave is not null)
                await afterSave(db, record);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting paid amount: {ex.Message}");
        }
    }

    // An entry marked as paid without partial payments counts as having its whole amount paid.
    private static decimal GetAlreadyPaid(FinancialMonthlyEntry record, decimal plannedAmount) =>
        record.PaidAmount ?? (record.Status == FinancialMonthlyEntryStatus.Paid ? plannedAmount : 0);

    public async Task DeleteEntryAsync(int entryId, int year, int month, EntryDeleteScope scope)
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            var template = await db.Table<FinancialEntry>().Where(e => e.Id == entryId).FirstOrDefaultAsync();
            if (template is null)
                return;

            var targetDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
            var firstOfStart = new DateTime(template.StartDate.Year, template.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Local);

            if (template.Recurrence == RecurrenceType.OneTime ||
                (scope == EntryDeleteScope.ThisAndFuture && targetDate <= firstOfStart))
                scope = EntryDeleteScope.All;

            var monthlyRecords = await db.Table<FinancialMonthlyEntry>()
                .Where(r => r.FinancialEntryId == entryId)
                .ToListAsync();

            switch (scope)
            {
                case EntryDeleteScope.ThisMonth:
                    var record = monthlyRecords.FirstOrDefault(r => r.Year == year && r.Month == month)
                        ?? new FinancialMonthlyEntry { FinancialEntryId = entryId, Year = year, Month = month };
                    record.IsCancelled = true;

                    if (record.Id != 0)
                        await db.UpdateAsync(record);
                    else
                        await db.InsertAsync(record);
                    break;

                case EntryDeleteScope.ThisAndFuture:
                    template.EndDate = targetDate.AddMonths(-1);
                    await db.UpdateAsync(template);

                    foreach (var r in monthlyRecords.Where(r => r.Year * 12 + r.Month >= year * 12 + month))
                        await DeleteMonthlyRecordAsync(db, r);
                    break;

                case EntryDeleteScope.All:
                    foreach (var r in monthlyRecords)
                        await DeleteMonthlyRecordAsync(db, r);

                    await db.DeleteAsync(template);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting entry: {ex.Message}");
        }
    }

    public async Task ClearAllDataAsync()
    {
        try
        {
            var db = await _context.GetConnectionAsync();

            await db.DeleteAllAsync<FinancialEntry>();
            await db.DeleteAllAsync<FinancialMonthlyEntry>();
            await db.DeleteAllAsync<FinancialPayment>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Erro ao limpar banco: {ex.Message}");
        }
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

    private static async Task<FinancialMonthlyEntry> GetOrCreateMonthlyRecordAsync(SQLiteAsyncConnection db, int entryId, int year, int month) =>
        await db.Table<FinancialMonthlyEntry>()
            .Where(r => r.FinancialEntryId == entryId && r.Year == year && r.Month == month)
            .FirstOrDefaultAsync()
        ?? new FinancialMonthlyEntry { FinancialEntryId = entryId, Year = year, Month = month };

    private static async Task UpsertAsync(SQLiteAsyncConnection db, FinancialMonthlyEntry record)
    {
        if (record.Id != 0)
            await db.UpdateAsync(record);
        else
            await db.InsertAsync(record);
    }

    private static async Task DeleteMonthlyRecordAsync(SQLiteAsyncConnection db, FinancialMonthlyEntry record)
    {
        await DeletePaymentsAsync(db, record.Id);
        await db.DeleteAsync(record);
    }

    private static Task DeletePaymentsAsync(SQLiteAsyncConnection db, int monthlyEntryId) =>
        db.Table<FinancialPayment>().DeleteAsync(p => p.FinancialMonthlyEntryId == monthlyEntryId);

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

            _ => false
        };
    }

    private static string? BuildInstallmentLabel(FinancialEntry entry, int year, int month)
    {
        if (entry.Recurrence != RecurrenceType.Installments)
            return null;

        int n = (year - entry.StartDate.Year) * 12 + (month - entry.StartDate.Month) + 1;
        return $"{n}/{entry.TotalInstallments}";
    }

    private static decimal? BuildInstallmentTotal(FinancialEntry entry) =>
        entry.Recurrence == RecurrenceType.Installments
            ? entry.Amount * entry.TotalInstallments
            : null;
}
