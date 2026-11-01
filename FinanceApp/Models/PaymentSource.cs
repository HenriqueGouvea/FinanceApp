namespace FinanceApp.Models;

/// <summary>
/// Where an entry's money comes from (or goes to): a bank account, a credit card or a meal voucher.
/// </summary>
public class PaymentSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PaymentSourceType Type { get; set; } = PaymentSourceType.BankAccount;
    /// <summary>
    /// The balance set by the user: the account or voucher balance, or the card's limit.
    /// </summary>
    public decimal Balance { get; set; }
}
