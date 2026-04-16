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

    public async Task<IEnumerable<FinancialEntry>> GetEntriesByMonthAsync(DateTime date)
    {
        var db = await _context.GetConnectionAsync();

        var startOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        return await db.Table<FinancialEntry>()
            .Where(e => e.Date >= startOfMonth && e.Date < startOfNextMonth)
            .ToListAsync();
    }

    public async Task SaveEntryAsync(FinancialEntry entry)
    {
        try
        {
            var db = await _context.GetConnectionAsync();
            var result = 0;

            if (entry.Id != 0)
            {
                result = await db.UpdateAsync(entry);
            }
            else
            {
                result = await db.InsertAsync(entry);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving entry: {ex.Message}");
        }
    }
}
