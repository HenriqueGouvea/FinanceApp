namespace FinanceApp.Models;

/// <summary>
/// Represents a consolidated view of a financial entry for a specific month.
/// This is a non-persisted projection that merges the 'template' (FinancialEntry) with its potential 'physical override' (FinancialMonthlyEntry).
/// </summary>
/// <param name="FinancialEntryId">The ID of the parent template.</param>
/// <param name="FinancialMonthlyEntryId">The ID of the physical record in the database, or null if it's purely projected.</param>
/// <param name="Description">The display description of the entry.</param>
/// <param name="EffectiveAmount">The final value to be displayed (the template amount or the monthly override, raised to the paid amount when more was paid).</param>
/// <param name="EntryType">Income or Outcome.</param>
/// <param name="Category">The assigned financial category, or null when uncategorized.</param>
/// <param name="Status">The current payment status (Pending, Paid, etc.).</param>
/// <param name="IsProjected">True if this occurrence exists only in memory; False if it has been persisted/modified in the DB.</param>
/// <param name="InstallmentLabel">Formatted string for installments (e.g., '02/10') or null for other types.</param>
/// <param name="InstallmentTotalAmount">Full amount of all installments (template amount × total installments), or null for other types.</param>
/// <param name="PaidAmount">Amount already paid (or received) in the month: the whole amount once paid, zero while pending.</param>
/// <param name="IsEstimated">True when the template amount is only an estimate.</param>
public record MonthlyEntryProjection(
    int FinancialEntryId,
    int? FinancialMonthlyEntryId,
    string Description,
    decimal EffectiveAmount,
    FinancialEntryType EntryType,
    FinancialCategory? Category,
    FinancialMonthlyEntryStatus Status,
    bool IsProjected,
    string? InstallmentLabel,
    decimal? InstallmentTotalAmount,
    decimal PaidAmount = 0,
    bool IsEstimated = false
)
{
    public bool IsPaid => Status == FinancialMonthlyEntryStatus.Paid;
    public bool IsPartiallyPaid => Status == FinancialMonthlyEntryStatus.PartiallyPaid;
    public bool IsPending => Status == FinancialMonthlyEntryStatus.Pending;
    public decimal PendingAmount => Math.Max(0, EffectiveAmount - PaidAmount);
}
