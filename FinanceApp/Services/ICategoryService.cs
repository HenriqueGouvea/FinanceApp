using FinanceApp.Models;

namespace FinanceApp.Services;

public interface ICategoryService
{
    /// <summary>
    /// All categories sorted by name. The list is cached and shared by every caller: don't change its items.
    /// </summary>
    Task<IReadOnlyList<FinancialCategory>> GetAllAsync();

    /// <summary>
    /// Drops the cached list, for writes made outside this service (backup import, wipe).
    /// </summary>
    void ClearCache();

    Task SaveAsync(FinancialCategory category);

    Task<int> CountEntriesAsync(int categoryId);

    /// <summary>
    /// Deletes the category and leaves the entries that used it uncategorized.
    /// </summary>
    Task DeleteAsync(int categoryId);
}
