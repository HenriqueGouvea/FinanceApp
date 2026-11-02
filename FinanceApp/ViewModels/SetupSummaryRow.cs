using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// An entry the setup wizard is about to create, as listed on its last step.
/// </summary>
public record SetupSummaryRow(string Icon, string Description, string Detail, decimal Amount, FinancialEntryType EntryType);
