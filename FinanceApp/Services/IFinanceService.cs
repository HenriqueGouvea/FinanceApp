using FinanceApp.Models;

namespace FinanceApp.Services;

public interface IFinanceService
{
    Task<IEnumerable<MonthlyEntryProjection>> GetMergedEntriesForMonthAsync(int year, int month);

    Task<FinancialEntry?> GetEntryByIdAsync(int id);

    Task SaveEntryAsync(FinancialEntry entry);

    Task SaveMonthlyEntryAsync(FinancialMonthlyEntry entry);

    /// <summary>
    /// Sets the entry's status for a single month, persisting its monthly record on first change.
    /// </summary>
    Task SetEntryStatusAsync(int entryId, int year, int month, FinancialMonthlyEntryStatus status);

    /// <summary>
    /// Records how much of the entry was already paid (or received) in a single month. The status follows:
    /// pending at zero, paid once the month's amount is reached, partially paid in between.
    /// </summary>
    Task SetPaidAmountAsync(int entryId, int year, int month, decimal paidAmount);

    Task DeleteEntryAsync(int entryId, int year, int month, EntryDeleteScope scope);

    /// <summary>
    /// A tool for the development phase only, which clears all data from the database.
    /// </summary>
    Task ClearAllDataAsync();
}
