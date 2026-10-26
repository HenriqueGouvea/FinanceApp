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
                var status = GetEffectiveStatus(record, plannedAmount);
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
                    IsEstimated: template.IsEstimated
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

            var record = await GetOrCreateMonthlyRecordAsync(db, entryId, year, month);
            record.Status = status;
            // Marking as paid or pending settles the whole amount, so a partial payment no longer applies.
            record.PaidAmount = null;

            await UpsertAsync(db, record);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting entry status: {ex.Message}");
        }
    }

    public Task SetPaidAmountAsync(int entryId, int year, int month, decimal paidAmount) =>
        UpdatePaidAmountAsync(entryId, year, month, (_, _) => paidAmount);

    public Task AddPaidAmountAsync(int entryId, int year, int month, decimal amount) =>
        UpdatePaidAmountAsync(entryId, year, month, (record, plannedAmount) =>
            (record.PaidAmount ?? (record.Status == FinancialMonthlyEntryStatus.Paid ? plannedAmount : 0)) + amount);

    private async Task UpdatePaidAmountAsync(int entryId, int year, int month, Func<FinancialMonthlyEntry, decimal, decimal> newPaidAmount)
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
            record.PaidAmount = paidAmount > 0 ? paidAmount : null;
            record.Status = GetEffectiveStatus(record, plannedAmount);

            await UpsertAsync(db, record);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting paid amount: {ex.Message}");
        }
    }

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
                        await db.DeleteAsync(r);
                    break;

                case EntryDeleteScope.All:
                    foreach (var r in monthlyRecords)
                        await db.DeleteAsync(r);

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Erro ao limpar banco: {ex.Message}");
        }
    }

    // A partial payment decides the status by itself, so it follows later changes to the month's amount.
    private static FinancialMonthlyEntryStatus GetEffectiveStatus(FinancialMonthlyEntry record, decimal plannedAmount) =>
        record.PaidAmount switch
        {
            > 0 and var paid when paid >= plannedAmount => FinancialMonthlyEntryStatus.Paid,
            > 0 => FinancialMonthlyEntryStatus.PartiallyPaid,
            null when record.Status == FinancialMonthlyEntryStatus.Paid => FinancialMonthlyEntryStatus.Paid,
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
