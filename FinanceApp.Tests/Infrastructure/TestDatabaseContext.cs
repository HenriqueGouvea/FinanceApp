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
        var connStr = new SQLiteConnectionString(
            $"file:{id}?mode=memory&cache=shared",
            storeDateTimeAsTicks: true);
        _connection = new SQLiteAsyncConnection(connStr);
        _connection.CreateTableAsync<FinancialEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
        _connection.CreateTableAsync<FinancialMonthlyEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK).Wait();
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => Task.FromResult(_connection);
}
