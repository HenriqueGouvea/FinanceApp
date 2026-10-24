namespace FinanceApp.UITests.Infrastructure;

// Black-box mirrors of the app's tables. Kept independent from the app's models on purpose,
// so a schema change that breaks stored data is caught here instead of being compiled away.

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
    public bool IsEstimated { get; set; }
}

public class FinancialMonthlyEntryRow
{
    public int Id { get; set; }
    public int FinancialEntryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Status { get; set; }
    public decimal? OverrideAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public bool IsCancelled { get; set; }
}

public class FinancialCategoryRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int EntryType { get; set; }
}
