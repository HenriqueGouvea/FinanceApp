using FinanceApp.Models;
using SQLite;

namespace FinanceApp.Data;

public class DatabaseContext : IDatabaseContext
{
    public const string MonthIndex = "IX_FinancialMonthlyEntry_Year_Month";
    public const string MonthlyEntryTemplateIndex = "IX_FinancialMonthlyEntry_FinancialEntryId";
    public const string PaymentMonthlyEntryIndex = "IX_FinancialPayment_FinancialMonthlyEntryId";
    public const string CategoryEntryEntryIndex = "IX_FinancialCategoryEntry_FinancialEntryId";
    public const string CategoryEntryCategoryIndex = "IX_FinancialCategoryEntry_FinancialCategoryId";

    // Databases created before category management stored the category name as text on FinancialEntry.
    private const string LegacyCategoryNameColumn = "Category";
    // Then, before an entry could have several categories, the id of its only one.
    private const string LegacyCategoryIdColumn = "CategoryId";

    private static readonly FinancialCategory[] DefaultOutcomeCategories =
    [
        new() { Name = "Moradia", Icon = "🏠", Color = "#1E88E5" },
        new() { Name = "Educação", Icon = "🎓", Color = "#8E24AA" },
        new() { Name = "Mercado", Icon = "🛒", Color = "#F4511E" },
        new() { Name = "Saúde", Icon = "🩺", Color = "#E53935" },
        new() { Name = "Transporte", Icon = "🚗", Color = "#6D4C41" },
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
        await connection.CreateTableAsync<PaymentSource>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);
        await connection.CreateTableAsync<SetupLink>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        if (entryColumns.Count > 0 && !hadPartialPaymentFlag)
            await MigratePartialPaymentFlagAsync(connection);

        var categoryColumns = await connection.GetTableInfoAsync(nameof(FinancialCategory));
        var hadCategoryType = categoryColumns.Any(c => c.Name == nameof(FinancialCategory.EntryType));
        var categoryTable = await connection.CreateTableAsync<FinancialCategory>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);
        var categoryEntryTable = await connection.CreateTableAsync<FinancialCategoryEntry>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex);

        if (categoryTable == CreateTableResult.Created)
            await SeedDefaultCategoriesAsync(connection);
        else if (!hadCategoryType)
            await MigrateCategoryTypesAsync(connection);

        if (categoryEntryTable == CreateTableResult.Created)
            await MigrateCategoryLinksAsync(connection);

        await CreateIndexesAsync(connection);

        return connection;
    }

    /// <summary>
    /// Indexes for the lookups run on every month load and payment, created only when missing.
    /// </summary>
    public static async Task CreateIndexesAsync(SQLiteAsyncConnection db)
    {
        await db.CreateIndexAsync(MonthIndex, nameof(FinancialMonthlyEntry),
            [nameof(FinancialMonthlyEntry.Year), nameof(FinancialMonthlyEntry.Month)]);
        await db.CreateIndexAsync(MonthlyEntryTemplateIndex, nameof(FinancialMonthlyEntry),
            [nameof(FinancialMonthlyEntry.FinancialEntryId)]);
        await db.CreateIndexAsync(PaymentMonthlyEntryIndex, nameof(FinancialPayment),
            [nameof(FinancialPayment.FinancialMonthlyEntryId)]);
        await db.CreateIndexAsync(CategoryEntryEntryIndex, nameof(FinancialCategoryEntry),
            [nameof(FinancialCategoryEntry.FinancialEntryId)]);
        await db.CreateIndexAsync(CategoryEntryCategoryIndex, nameof(FinancialCategoryEntry),
            [nameof(FinancialCategoryEntry.FinancialCategoryId)]);
    }

    /// <summary>
    /// Links each entry to the category kept in the legacy columns of <see cref="FinancialEntry"/> (its name as text,
    /// or its id), as its main category. Entries that already have links are left as they are.
    /// </summary>
    public static async Task MigrateCategoryLinksAsync(SQLiteAsyncConnection db)
    {
        var columns = await db.GetTableInfoAsync(nameof(FinancialEntry));
        var match = columns.Any(c => c.Name == LegacyCategoryIdColumn)
            ? $"c.{nameof(FinancialCategory.Id)} = e.{LegacyCategoryIdColumn}"
            : columns.Any(c => c.Name == LegacyCategoryNameColumn)
                ? $"c.{nameof(FinancialCategory.Name)} = e.{LegacyCategoryNameColumn}"
                : null;
        if (match is null)
            return;

        await db.ExecuteAsync(
            $"INSERT INTO {nameof(FinancialCategoryEntry)} ({nameof(FinancialCategoryEntry.FinancialEntryId)}, " +
            $"{nameof(FinancialCategoryEntry.FinancialCategoryId)}, {nameof(FinancialCategoryEntry.Position)}) " +
            $"SELECT e.{nameof(FinancialEntry.Id)}, MIN(c.{nameof(FinancialCategory.Id)}), 0 " +
            $"FROM {nameof(FinancialEntry)} e JOIN {nameof(FinancialCategory)} c ON {match} " +
            $"WHERE e.{nameof(FinancialEntry.Id)} NOT IN (SELECT {nameof(FinancialCategoryEntry.FinancialEntryId)} FROM {nameof(FinancialCategoryEntry)}) " +
            $"GROUP BY e.{nameof(FinancialEntry.Id)}");
    }

    /// <summary>
    /// Fresh copies of the categories a new install starts with, outcomes first.
    /// </summary>
    public static IReadOnlyList<FinancialCategory> CreateDefaultCategories() =>
        [.. Copy([.. DefaultOutcomeCategories, .. DefaultIncomeCategories])];

    private static Task SeedDefaultCategoriesAsync(SQLiteAsyncConnection db) =>
        db.InsertAllAsync(CreateDefaultCategories());

    // Categories created before the income/outcome split were all used for outcomes.
    private static async Task MigrateCategoryTypesAsync(SQLiteAsyncConnection db)
    {
        await db.ExecuteAsync(
            $"UPDATE {nameof(FinancialCategory)} SET {nameof(FinancialCategory.EntryType)} = ?",
            (int)FinancialEntryType.Outcome);
        await db.InsertAllAsync(Copy(DefaultIncomeCategories));
    }

    // Before the flag every entry accepted partial payments: keep it on for entries that already received some.
    private static Task MigratePartialPaymentFlagAsync(SQLiteAsyncConnection db) =>
        db.ExecuteAsync(
            $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.AllowsPartialPayment)} = 1 " +
            $"WHERE {nameof(FinancialEntry.Id)} IN (SELECT {nameof(FinancialMonthlyEntry.FinancialEntryId)} FROM {nameof(FinancialMonthlyEntry)} " +
            $"WHERE {nameof(FinancialMonthlyEntry.PaidAmount)} IS NOT NULL)");

    private static IEnumerable<FinancialCategory> Copy(IEnumerable<FinancialCategory> categories) =>
        categories.Select(c => new FinancialCategory
        {
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            EntryType = c.EntryType
        });
}
