namespace FinanceApp.Models;

/// <summary>
/// A single partial payment (or receipt) recorded against a month of an entry, e.g. one supermarket bill.
/// </summary>
public class FinancialPayment
{
    public int Id { get; set; }
    public int FinancialMonthlyEntryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
}
