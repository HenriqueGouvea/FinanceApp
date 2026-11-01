using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class PaymentSourceService : IPaymentSourceService
{
    private readonly IDatabaseContext _context;

    public PaymentSourceService(IDatabaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PaymentSource>> GetAllAsync()
    {
        var db = await _context.GetConnectionAsync();
        var sources = await db.Table<PaymentSource>().ToListAsync();

        return sources
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task SaveAsync(PaymentSource source)
    {
        var db = await _context.GetConnectionAsync();
        source.Name = source.Name.Trim();

        if (source.Id != 0)
            await db.UpdateAsync(source);
        else
            await db.InsertAsync(source);
    }

    public async Task<int> CountEntriesAsync(int sourceId)
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().Where(e => e.PaymentSourceId == sourceId).CountAsync();
    }

    public async Task DeleteAsync(int sourceId)
    {
        var db = await _context.GetConnectionAsync();

        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute(
                $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.PaymentSourceId)} = NULL WHERE {nameof(FinancialEntry.PaymentSourceId)} = ?",
                sourceId);
            conn.Delete<PaymentSource>(sourceId);
        });
    }
}
