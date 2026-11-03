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
    /// <summary>
    /// Credit cards only: day of the month (1–31) the card's bill falls due, or null when not set.
    /// Days past the end of a month fall on its last day (e.g. 31 is February 28th).
    /// </summary>
    public int? BillDueDay { get; set; }
    /// <summary>
    /// Credit cards only: the bank account that pays the card's bill, or null when not set.
    /// </summary>
    public int? BillPaymentSourceId { get; set; }
}
