using FinanceApp.Models;

namespace FinanceApp.Services;

public interface IFinanceService
{
    Task<IEnumerable<FinancialEntry>> GetEntriesByMonthAsync(DateTime date);
    Task SaveEntryAsync(FinancialEntry entry);
}
