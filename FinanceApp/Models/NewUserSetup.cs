namespace FinanceApp.Models;

/// <summary>
/// What the user filled in the setup wizard, turned into data by <see cref="Services.ISetupService.CompleteAsync"/>.
/// </summary>
public class NewUserSetup
{
    public string Name { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Accounts, cards and vouchers to create. Their ids are set once saved, so <see cref="Entries"/> can point to them.
    /// </summary>
    public List<PaymentSource> PaymentSources { get; set; } = [];

    public List<SetupEntry> Entries { get; set; } = [];
}

/// <summary>
/// A monthly income or expense from the setup wizard, created as a recurrent entry starting in the current month.
/// </summary>
public class SetupEntry
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FinancialEntryType EntryType { get; set; } = FinancialEntryType.Outcome;
    public int? DueDay { get; set; }
    public bool IsEstimated { get; set; }
    public bool AllowsPartialPayment { get; set; }

    /// <summary>
    /// Name of the category to file the entry under. The entry is left uncategorized when no category has that name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// An existing source or one of <see cref="NewUserSetup.PaymentSources"/>.
    /// </summary>
    public PaymentSource? PaymentSource { get; set; }
}
