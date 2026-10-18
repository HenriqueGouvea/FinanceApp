namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Black-box mirror of the FinancialEntry table. Kept independent from the app's models on purpose,
/// so a schema change that breaks stored data is caught here instead of being compiled away.
/// </summary>
public class FinancialEntryRow
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Recurrence { get; set; }
    public int EntryType { get; set; }
    public int? TotalInstallments { get; set; }
    public int? CategoryId { get; set; }
}
