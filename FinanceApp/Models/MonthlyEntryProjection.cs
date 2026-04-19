namespace FinanceApp.Models;

public record MonthlyEntryProjection(
    int FinancialEntryId,
    int? FinancialMonthlyEntryId,
    string Description,
    decimal EffectiveAmount,
    FinancialEntryType EntryType,
    string Category,
    FinancialMonthlyEntryStatus Status,
    bool IsProjected,
    string? InstallmentLabel
);
