using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A purchase already on a credit card, added in the setup wizard: made once this month ("À vista"), in installments
/// (<paramref name="CurrentInstallment"/> of <paramref name="TotalInstallments"/> falls this month) or every month,
/// like a subscription.
/// <paramref name="ExistingEntryId"/> is the entry an earlier run created, which saving updates.
/// </summary>
public record SetupPurchaseItem(
    string Description,
    decimal Amount,
    RecurrenceType Recurrence,
    PaymentSource Card,
    int? TotalInstallments = null,
    int? CurrentInstallment = null,
    int? ExistingEntryId = null)
{
    /// <summary>
    /// "Parcela 3 de 10 · Visa".
    /// </summary>
    public string Detail => $"{SetupViewModel.DescribeRecurrence(Recurrence, TotalInstallments, CurrentInstallment)} · {Card.Name}";
}
