using SQLite;

namespace FinanceApp.Data;

public interface IDatabaseContext
{
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
