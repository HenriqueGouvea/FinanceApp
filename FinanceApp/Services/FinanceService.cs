using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class FinanceService : IFinanceService
{
    private readonly DatabaseContext _context;

    public FinanceService(DatabaseContext context)
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
