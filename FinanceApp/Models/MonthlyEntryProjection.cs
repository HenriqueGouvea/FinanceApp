namespace FinanceApp.Models;

/// <summary>
/// Represents a consolidated view of a financial entry for a specific month.
/// This is a non-persisted projection that merges the 'template' (FinancialEntry) with its potential 'physical override' (FinancialMonthlyEntry).
/// </summary>
/// <param name="FinancialEntryId">The ID of the parent template.</param>
/// <param name="FinancialMonthlyEntryId">The ID of the physical record in the database, or null if it's purely projected.</param>
/// <param name="Description">The display description of the entry.</param>
/// <param name="EffectiveAmount">The final value to be displayed (either the template amount or the monthly override).</param>
/// <param name="EntryType">Income or Outcome.</param>
/// <param name="Category">The assigned financial category.</param>
/// <param name="Status">The current payment status (Pending, Paid, etc.).</param>
/// <param name="IsProjected">True if this occurrence exists only in memory; False if it has been persisted/modified in the DB.</param>
/// <param name="InstallmentLabel">Formatted string for installments (e.g., '02/10') or null for other types.</param>
public record MonthlyEntryProjection(
    int FinancialEntryId,
    int? FinancialMonthlyEntryId,
    string Description,
    decimal EffectiveAmount,
    FinancialEntryType EntryType,
    string Category,
    FinancialMonthlyEntryStatus Status,
    bool IsProjected,
    string? InstallmentLabel
);
