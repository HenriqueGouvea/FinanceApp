using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Data;

public class DatabaseContext : IDatabaseContext
{
    private const string LegacyCategoryColumn = "Category";

    private static readonly FinancialCategory[] DefaultOutcomeCategories =
    [
        new() { Name = "Moradia", Icon = "🏠", Color = "#1E88E5" },
        new() { Name = "Educação", Icon = "🎓", Color = "#8E24AA" },
        new() { Name = "Mercado", Icon = "🛒", Color = "#F4511E" },
        new() { Name = "Lazer", Icon = "🎉", Color = "#43A047" }
    ];

    private static readonly FinancialCategory[] DefaultIncomeCategories =
    [
        new() { Name = "Salário", Icon = "💼", Color = "#00897B", EntryType = FinancialEntryType.Income },
        new() { Name = "Investimentos", Icon = "📈", Color = "#3949AB", EntryType = FinancialEntryType.Income }
    ];

    // Concurrent first callers (e.g. overlapping month loads at startup) must all await the same
    // initialization, otherwise they can query tables that are still being created.
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connection;
    private readonly string _databasePath;

    public DatabaseContext()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinanceDataV3.db3");
#else
        _databasePath = Path.Combine(Path.GetTempPath(), "FinanceDataV3.db3");
#endif
        _connection = new Lazy<Task<SQLiteAsyncConnection>>(InitializeConnectionAsync);
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => _connection.Value;

    private async Task<SQLiteAsyncConnection> InitializeConnectionAsync()
    {
        var connection = new SQLiteAsyncConnection(_databasePath);

        var entryColumns = await connection.GetTableInfoAsync(nameof(FinancialEntry));
        var hadPartialPaymentFlag = entryColumns.Any(c => c.Name == nameof(FinancialEntry.AllowsPartialPayment));
        await connection.CreateTableAsync<FinancialEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);
        await connection.CreateTableAsync<FinancialMonthlyEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);
        await connection.CreateTableAsync<FinancialPayment>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        if (entryColumns.Count > 0 && !hadPartialPaymentFlag)
            await MigratePartialPaymentFlagAsync(connection);

        var categoryColumns = await connection.GetTableInfoAsync(nameof(FinancialCategory));
        var hadCategoryType = categoryColumns.Any(c => c.Name == nameof(FinancialCategory.EntryType));
        var categoryTable = await connection.CreateTableAsync<FinancialCategory>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        if (categoryTable == CreateTableResult.Created)
            await SeedDefaultCategoriesAsync(connection);
        else if (!hadCategoryType)
            await MigrateCategoryTypesAsync(connection);

        return connection;
    }

    private static async Task SeedDefaultCategoriesAsync(SQLiteAsyncConnection db)
    {
        await InsertCopiesAsync(db, [.. DefaultOutcomeCategories, .. DefaultIncomeCategories]);

        // Databases created before category management stored the category name as text on FinancialEntry.
        var columns = await db.GetTableInfoAsync(nameof(FinancialEntry));
        if (columns.Any(c => c.Name == LegacyCategoryColumn))
            await db.ExecuteAsync(
                $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.CategoryId)} = " +
                $"(SELECT {nameof(FinancialCategory.Id)} FROM {nameof(FinancialCategory)} WHERE {nameof(FinancialCategory.Name)} = {nameof(FinancialEntry)}.{LegacyCategoryColumn}) " +
                $"WHERE {nameof(FinancialEntry.CategoryId)} IS NULL");
    }

    // Categories created before the income/outcome split were all used for outcomes.
    private static async Task MigrateCategoryTypesAsync(SQLiteAsyncConnection db)
    {
        await db.ExecuteAsync(
            $"UPDATE {nameof(FinancialCategory)} SET {nameof(FinancialCategory.EntryType)} = ?",
            (int)FinancialEntryType.Outcome);
        await InsertCopiesAsync(db, DefaultIncomeCategories);
    }

    // Before the flag every entry accepted partial payments: keep it on for entries that already received some.
    private static Task MigratePartialPaymentFlagAsync(SQLiteAsyncConnection db) =>
        db.ExecuteAsync(
            $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.AllowsPartialPayment)} = 1 " +
            $"WHERE {nameof(FinancialEntry.Id)} IN (SELECT {nameof(FinancialMonthlyEntry.FinancialEntryId)} FROM {nameof(FinancialMonthlyEntry)} " +
            $"WHERE {nameof(FinancialMonthlyEntry.PaidAmount)} IS NOT NULL)");

    private static Task InsertCopiesAsync(SQLiteAsyncConnection db, IEnumerable<FinancialCategory> categories) =>
        db.InsertAllAsync(categories.Select(c => new FinancialCategory
        {
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            EntryType = c.EntryType
        }));
}
