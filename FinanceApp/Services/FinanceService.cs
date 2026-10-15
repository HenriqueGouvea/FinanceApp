using FinanceApp.Data;
using FinanceApp.Models;

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

        var projections = new List<MonthlyEntryProjection>();

        foreach (var template in activeTemplates)
        {
            if (physicalLookup.TryGetValue(template.Id, out var record))
            {
                if (record.IsCancelled)
                    continue;

                projections.Add(new MonthlyEntryProjection(
                    FinancialEntryId: template.Id,
                    FinancialMonthlyEntryId: record.Id,
                    Description: template.Description,
                    EffectiveAmount: record.OverrideAmount ?? template.Amount,
                    EntryType: template.EntryType,
                    Category: template.Category,
                    Status: record.Status,
                    IsProjected: false,
                    InstallmentLabel: BuildInstallmentLabel(template, year, month)
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
                    Category: template.Category,
                    Status: FinancialMonthlyEntryStatus.Pending,
                    IsProjected: true,
                    InstallmentLabel: BuildInstallmentLabel(template, year, month)
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
}
