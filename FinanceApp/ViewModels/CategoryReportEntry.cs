using FinanceApp.Converters;
using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// One of a category's entries in the month, listed when the user taps the category in the report.
/// </summary>
/// <param name="Description">The entry's description, with its installment (e.g. "Notebook 3/10").</param>
/// <param name="Amount">The entry's amount in the month.</param>
/// <param name="StatusLabel">"Pago"/"Recebido", "Pendente"/"A receber", or what was paid so far ("Pago R$ 450,00").</param>
/// <param name="Status">The entry's status in the month, which colors the label.</param>
public record CategoryReportEntry(string Description, decimal Amount, string StatusLabel, FinancialMonthlyEntryStatus Status)
{
    public bool IsPaid => Status == FinancialMonthlyEntryStatus.Paid;
    public bool IsPartiallyPaid => Status == FinancialMonthlyEntryStatus.PartiallyPaid;
    public bool IsPending => Status == FinancialMonthlyEntryStatus.Pending;

    public static CategoryReportEntry From(MonthlyEntryProjection entry)
    {
        var isIncome = entry.EntryType == FinancialEntryType.Income;
        var description = entry.InstallmentLabel is null ? entry.Description : $"{entry.Description} {entry.InstallmentLabel}";
        var status = entry.Status switch
        {
            FinancialMonthlyEntryStatus.Paid => isIncome ? "Recebido" : "Pago",
            FinancialMonthlyEntryStatus.PartiallyPaid => $"{(isIncome ? "Recebido" : "Pago")} {MoneyConverter.Format(entry.PaidAmount)}",
            _ => isIncome ? "A receber" : "Pendente"
        };

        return new CategoryReportEntry(description, entry.EffectiveAmount, status, entry.Status);
    }
}
