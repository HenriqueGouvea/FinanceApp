namespace FinanceApp.Models;

/// <summary>
/// What the user filled in the setup wizard, turned into data by <see cref="Services.ISetupService.CompleteAsync"/>.
/// </summary>
public class NewUserSetup
{
    public string Name { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Accounts and vouchers to create. Their ids are set once saved, so <see cref="CreditCards"/> and
    /// <see cref="Entries"/> can point to them.
    /// </summary>
    public List<PaymentSource> PaymentSources { get; set; } = [];

    /// <summary>
    /// Credit cards to create, after <see cref="PaymentSources"/> so they can point to the account that pays their bill.
    /// </summary>
    public List<SetupCreditCard> CreditCards { get; set; } = [];

    public List<SetupEntry> Entries { get; set; } = [];
}

/// <summary>
/// A credit card from the setup wizard and the account that pays its bill.
/// </summary>
public class SetupCreditCard
{
    public PaymentSource Card { get; set; } = new() { Type = PaymentSourceType.CreditCard };

    /// <summary>
    /// An existing bank account or one of <see cref="NewUserSetup.PaymentSources"/>, or null when not set.
    /// </summary>
    public PaymentSource? BillAccount { get; set; }
}

/// <summary>
/// An income or expense from the setup wizard: by default a recurrent entry starting in the current month. A purchase
/// already on a credit card can also be a one-time entry in the current month or an installment purchase in progress.
/// </summary>
public class SetupEntry
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FinancialEntryType EntryType { get; set; } = FinancialEntryType.Outcome;
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.Recurrent;

    /// <summary>
    /// Installments: how many installments the purchase has. Annual: how many installments fall each year, in
    /// consecutive months (one when not set).
    /// </summary>
    public int? TotalInstallments { get; set; }

    /// <summary>
    /// Annual only: the month (1–12) of the first installment each year, e.g. 1 for an IPVA paid from January.
    /// The entry starts this year while any of this year's installments is still to come, otherwise next year.
    /// </summary>
    public int? AnnualStartMonth { get; set; }

    /// <summary>
    /// Installments only: the installment that falls in the current month (e.g. 3 for "3/10"), so the entry starts
    /// that many months earlier. Defaults to the first one.
    /// </summary>
    public int? CurrentInstallment { get; set; }

    public int? DueDay { get; set; }
    public bool IsEstimated { get; set; }
    public bool AllowsPartialPayment { get; set; }

    /// <summary>
    /// Name of the category to file the entry under. The entry is left uncategorized when no category has that name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// An existing source, one of <see cref="NewUserSetup.PaymentSources"/> or the card of one of
    /// <see cref="NewUserSetup.CreditCards"/>.
    /// </summary>
    public PaymentSource? PaymentSource { get; set; }
}
