using FinanceApp.Models;

namespace FinanceApp.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<FinancialCategory>> GetAllAsync();

    Task SaveAsync(FinancialCategory category);

    Task<int> CountEntriesAsync(int categoryId);

    /// <summary>
    /// Deletes the category and leaves the entries that used it uncategorized.
    /// </summary>
    Task DeleteAsync(int categoryId);
}
