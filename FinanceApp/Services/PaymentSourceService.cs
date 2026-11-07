using FinanceApp.Data;
using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Services;

public class PaymentSourceService : IPaymentSourceService
{
    private const string UnlinkBillPaymentsSql =
        $"UPDATE {nameof(PaymentSource)} SET {nameof(PaymentSource.BillPaymentSourceId)} = NULL WHERE {nameof(PaymentSource.BillPaymentSourceId)} = ?";

    private readonly IDatabaseContext _context;
    private Task<IReadOnlyList<PaymentSource>>? _cache;

    public PaymentSourceService(IDatabaseContext context)
    {
        _context = context;
    }

    // Every month load needs the payment sources: read them once and keep them until one is written.
    // Concurrent first callers share the same read, and a failed read is retried on the next call.
    public Task<IReadOnlyList<PaymentSource>> GetAllAsync()
    {
        var cache = _cache;
        if (cache is null || cache.IsFaulted || cache.IsCanceled)
            _cache = cache = LoadAllAsync();

        return cache;
    }

    public void ClearCache() => _cache = null;

    private async Task<IReadOnlyList<PaymentSource>> LoadAllAsync()
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
        Normalize(source);

        await db.RunInTransactionAsync(conn =>
        {
            if (source.Id != 0)
                conn.Update(source);
            else
                conn.Insert(source);

            // Only bank accounts pay bills: a source that stopped being one no longer pays any card's bill.
            if (source.Type != PaymentSourceType.BankAccount)
                conn.Execute(UnlinkBillPaymentsSql, source.Id);
        });

        ClearCache();
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
            conn.Execute(UnlinkBillPaymentsSql, sourceId);
            conn.Delete<PaymentSource>(sourceId);
        });

        ClearCache();
    }

    internal static void Normalize(PaymentSource source)
    {
        source.Name = source.Name.Trim();

        // Only credit cards have a bill.
        if (source.Type != PaymentSourceType.CreditCard)
        {
            source.BillDueDay = null;
            source.BillPaymentSourceId = null;
        }
    }
}
