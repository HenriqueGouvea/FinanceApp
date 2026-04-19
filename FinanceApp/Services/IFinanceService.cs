using FinanceApp.Models;

namespace FinanceApp.Services;

public interface IFinanceService
{
    Task<IEnumerable<MonthlyEntryProjection>> GetMergedEntriesForMonthAsync(int year, int month);

    Task SaveEntryAsync(FinancialEntry entry);

    Task SaveMonthlyEntryAsync(FinancialMonthlyEntry entry);
    
    /// <summary>
    /// A tool for the development phase only, which clears all data from the database.
    /// </summary>
    Task ClearAllDataAsync();
}
