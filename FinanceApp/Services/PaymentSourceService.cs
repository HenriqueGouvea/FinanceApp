using FinanceApp.Data;
using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Services;

public class PaymentSourceService : IPaymentSourceService
{
    private const string UnlinkBillPaymentsSql =
        $"UPDATE {nameof(PaymentSource)} SET {nameof(PaymentSource.BillPaymentSourceId)} = NULL WHERE {nameof(PaymentSource.BillPaymentSourceId)} = ?";

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

        // Only credit cards have a bill.
        if (source.Type != PaymentSourceType.CreditCard)
        {
            source.BillDueDay = null;
            source.BillPaymentSourceId = null;
        }

        if (source.Id != 0)
            await db.UpdateAsync(source);
        else
            await db.InsertAsync(source);

        // Only bank accounts pay bills: a source that stopped being one no longer pays any card's bill.
        if (source.Type != PaymentSourceType.BankAccount)
            await UnlinkBillPaymentsAsync(db, source.Id);
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
    }

    private static Task UnlinkBillPaymentsAsync(SQLiteAsyncConnection db, int accountId) =>
        db.ExecuteAsync(UnlinkBillPaymentsSql, accountId);
}
