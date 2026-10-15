using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Data;

public class DatabaseContext : IDatabaseContext
{
    private const string LegacyCategoryColumn = "Category";

    private static readonly FinancialCategory[] DefaultCategories =
    [
        new() { Name = "Moradia", Icon = "🏠", Color = "#1E88E5" },
        new() { Name = "Educação", Icon = "🎓", Color = "#8E24AA" },
        new() { Name = "Mercado", Icon = "🛒", Color = "#F4511E" },
        new() { Name = "Lazer", Icon = "🎉", Color = "#43A047" }
    ];

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
        var categoryTable = await _connection.CreateTableAsync<FinancialCategory>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        if (categoryTable == CreateTableResult.Created)
            await SeedDefaultCategoriesAsync(_connection);

        return _connection;
    }

    private static async Task SeedDefaultCategoriesAsync(SQLiteAsyncConnection db)
    {
        await db.InsertAllAsync(DefaultCategories.Select(c => new FinancialCategory { Name = c.Name, Icon = c.Icon, Color = c.Color }));

        // Databases created before category management stored the category name as text on FinancialEntry.
        var columns = await db.GetTableInfoAsync(nameof(FinancialEntry));
        if (columns.Any(c => c.Name == LegacyCategoryColumn))
            await db.ExecuteAsync(
                $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.CategoryId)} = " +
                $"(SELECT {nameof(FinancialCategory.Id)} FROM {nameof(FinancialCategory)} WHERE {nameof(FinancialCategory.Name)} = {nameof(FinancialEntry)}.{LegacyCategoryColumn}) " +
                $"WHERE {nameof(FinancialEntry.CategoryId)} IS NULL");
    }
}
