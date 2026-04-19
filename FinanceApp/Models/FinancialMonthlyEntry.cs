namespace FinanceApp.Models;

public class FinancialMonthlyEntry
{
    public int Id { get; set; }
    public int FinancialEntryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public FinancialMonthlyEntryStatus Status { get; set; }
    public decimal? OverrideAmount { get; set; }
    public bool IsCancelled { get; set; }
}
