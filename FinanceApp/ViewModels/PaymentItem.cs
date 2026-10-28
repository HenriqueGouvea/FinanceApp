using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A recorded payment as listed in the partial payment popup.
/// </summary>
public record PaymentItem(int Id, decimal Amount, DateTime Date, string Description)
{
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public static PaymentItem From(FinancialPayment payment) =>
        new(payment.Id, payment.Amount, payment.Date, payment.Description);
}
