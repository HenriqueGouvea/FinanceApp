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
    /// Paid settles the whole planned amount (keeping a total paid above it); pending resets the month.
    /// </summary>
    Task SetEntryStatusAsync(int entryId, int year, int month, FinancialMonthlyEntryStatus status);

    /// <summary>
    /// Records how much of the entry was already paid (or received) in a single month: partially paid while above zero,
    /// even past the planned amount, since only <see cref="SetEntryStatusAsync"/> settles a month. A settled month stays
    /// paid unless the new total is below its planned amount. Clearing it (zero) reverts to pending and removes the
    /// month's payment records.
    /// </summary>
    Task SetPaidAmountAsync(int entryId, int year, int month, decimal paidAmount);

    /// <summary>
    /// Records a payment in a single month and adds it to what was already paid (or received), with the same
    /// status rules as <see cref="SetPaidAmountAsync"/>.
    /// </summary>
    Task AddPaymentAsync(int entryId, int year, int month, decimal amount, DateTime date, string description);

    /// <summary>
    /// The payments recorded in a single month of the entry, oldest first.
    /// </summary>
    Task<IReadOnlyList<FinancialPayment>> GetPaymentsAsync(int entryId, int year, int month);

    /// <summary>
    /// Removes a payment record and subtracts it from what was paid in its month.
    /// </summary>
    Task DeletePaymentAsync(int paymentId);

    Task DeleteEntryAsync(int entryId, int year, int month, EntryDeleteScope scope);

    /// <summary>
    /// A tool for the development phase only, which clears all data from the database.
    /// </summary>
    Task ClearAllDataAsync();
}
