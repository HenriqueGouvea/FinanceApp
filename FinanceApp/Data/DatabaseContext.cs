using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Data;

public class DatabaseContext
{
    private SQLiteAsyncConnection? _connection;
    private readonly string _databasePath;

    public DatabaseContext()
    {
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinanceDataV2.db3");
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SQLiteAsyncConnection(_databasePath);

        await _connection.CreateTableAsync<FinancialEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        return _connection;
    }
}
