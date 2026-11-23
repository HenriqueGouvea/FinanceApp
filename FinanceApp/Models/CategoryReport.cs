namespace FinanceApp.Models;

/// <summary>
/// A month's incomes and outcomes added up by category (non-persisted read model of the category report).
/// </summary>
/// <param name="Totals">Each category's total, incomes first and the largest first; the uncategorized entries are under a null category.</param>
/// <param name="TotalIncomes">The month's incomes, each entry counted once.</param>
/// <param name="TotalOutcomes">The month's outcomes, each entry counted once.</param>
public record CategoryReport(IReadOnlyList<CategoryTotal> Totals, decimal TotalIncomes, decimal TotalOutcomes)
{
    public IEnumerable<CategoryTotal> TotalsOf(FinancialEntryType type) => Totals.Where(t => t.EntryType == type);

    public decimal MonthTotalOf(FinancialEntryType type) => type == FinancialEntryType.Income ? TotalIncomes : TotalOutcomes;
}
