namespace FinanceApp.Models;

public class FinancialEntry
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    /// <summary>
    /// Last month (inclusive) in which the entry is active. Null means no end boundary.
    /// </summary>
    public DateTime? EndDate { get; set; }
    public RecurrenceType Recurrence { get; set; }
    public FinancialEntryType EntryType { get; set; }
    public int? TotalInstallments { get; set; }
    public int? CategoryId { get; set; }
    public string Person { get; set; } = string.Empty;
    public bool IsPJ { get; set; }
}
