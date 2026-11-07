using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class CategoryService : ICategoryService
{
    private readonly IDatabaseContext _context;
    private Task<IReadOnlyList<FinancialCategory>>? _cache;

    public CategoryService(IDatabaseContext context)
    {
        _context = context;
    }

    // Every month load needs the categories: read them once and keep them until one is written.
    // Concurrent first callers share the same read, and a failed read is retried on the next call.
    public Task<IReadOnlyList<FinancialCategory>> GetAllAsync()
    {
        var cache = _cache;
        if (cache is null || cache.IsFaulted || cache.IsCanceled)
            _cache = cache = LoadAllAsync();

        return cache;
    }

    public void ClearCache() => _cache = null;

    private async Task<IReadOnlyList<FinancialCategory>> LoadAllAsync()
    {
        var db = await _context.GetConnectionAsync();
        var categories = await db.Table<FinancialCategory>().ToListAsync();

        return categories
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task SaveAsync(FinancialCategory category)
    {
        var db = await _context.GetConnectionAsync();
        category.Name = category.Name.Trim();

        if (category.Id != 0)
            await db.UpdateAsync(category);
        else
            await db.InsertAsync(category);

        ClearCache();
    }

    public async Task<int> CountEntriesAsync(int categoryId)
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().Where(e => e.CategoryId == categoryId).CountAsync();
    }

    public async Task DeleteAsync(int categoryId)
    {
        var db = await _context.GetConnectionAsync();

        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute(
                $"UPDATE {nameof(FinancialEntry)} SET {nameof(FinancialEntry.CategoryId)} = NULL WHERE {nameof(FinancialEntry.CategoryId)} = ?",
                categoryId);
            conn.Delete<FinancialCategory>(categoryId);
        });

        ClearCache();
    }
}
