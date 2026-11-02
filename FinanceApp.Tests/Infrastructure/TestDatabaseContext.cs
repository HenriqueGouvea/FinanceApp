using FinanceApp.Data;
using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Tests.Infrastructure;

public class TestDatabaseContext : IDatabaseContext
{
    private readonly SQLiteAsyncConnection _connection;

    public TestDatabaseContext()
    {
        var id = Guid.NewGuid().ToString("N");
        // Without the Uri flag the string is taken as a file name, which Windows rejects and Linux writes to disk.
        var connStr = new SQLiteConnectionString(
            $"file:{id}?mode=memory&cache=shared",
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Uri,
            storeDateTimeAsTicks: true);
        _connection = new SQLiteAsyncConnection(connStr);
        _connection.CreateTableAsync<FinancialEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
        _connection.CreateTableAsync<FinancialMonthlyEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
        _connection.CreateTableAsync<FinancialPayment>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
        _connection.CreateTableAsync<FinancialCategory>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
        _connection.CreateTableAsync<PaymentSource>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => Task.FromResult(_connection);
}
