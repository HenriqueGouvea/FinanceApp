namespace FinanceApp.Models;

public class FinancialMonthlyEntry
{
    public int Id { get; set; }
    public int FinancialEntryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public FinancialMonthlyEntryStatus Status { get; set; }
    public decimal? OverrideAmount { get; set; }
    /// <summary>
    /// Amount already paid (or received) in the month, for entries settled progressively (e.g. supermarket).
    /// </summary>
    public decimal? PaidAmount { get; set; }
    public bool IsCancelled { get; set; }
}
