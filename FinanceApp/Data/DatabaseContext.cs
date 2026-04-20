using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Data;

public class DatabaseContext : IDatabaseContext
{
    private SQLiteAsyncConnection? _connection;
    private readonly string _databasePath;

    public DatabaseContext()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinanceDataV3.db3");
#else
        _databasePath = Path.Combine(Path.GetTempPath(), "FinanceDataV3.db3");
#endif
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SQLiteAsyncConnection(_databasePath);

        await _connection.CreateTableAsync<FinancialEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);
        await _connection.CreateTableAsync<FinancialMonthlyEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        return _connection;
    }
}
