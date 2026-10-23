namespace FinanceApp.Models;

public class FinancialCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public FinancialEntryType EntryType { get; set; } = FinancialEntryType.Outcome;
}
