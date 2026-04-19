namespace FinanceApp.Models;

public class FinancialEntry
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public RecurrenceType Recurrence { get; set; }
    public FinancialEntryType EntryType { get; set; }
    public int? TotalInstallments { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Person { get; set; } = string.Empty;
    public bool IsPJ { get; set; }
}
