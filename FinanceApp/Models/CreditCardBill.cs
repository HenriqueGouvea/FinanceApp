namespace FinanceApp.Models;

/// <summary>
/// A credit card's bill for a month: the month's outcomes paid with the card, settled together on the card's due date.
/// This is a non-persisted read model grouped from <see cref="MonthlyEntryProjection"/> rows, so it never adds to the month's totals.
/// </summary>
/// <param name="Card">The credit card the entries were paid with.</param>
/// <param name="PaymentAccount">The bank account that pays the bill, or null when not set.</param>
/// <param name="DueDate">The day the bill falls due in the month, or null when the card has no due day.</param>
/// <param name="Entries">The month's outcomes paid with the card.</param>
/// <param name="IsExpanded">True while the bill lists its entries under it.</param>
public record CreditCardBill(
    PaymentSource Card,
    PaymentSource? PaymentAccount,
    DateTime? DueDate,
    IReadOnlyList<MonthlyEntryProjection> Entries,
    bool IsExpanded = true)
{
    public decimal TotalAmount => Entries.Sum(e => e.EffectiveAmount);
    public decimal PaidAmount => Entries.Sum(e => e.PaidAmount);
    public int EntryCount => Entries.Count;

    /// <summary>
    /// Paid once every entry is paid, pending while none has been paid, partially paid in between.
    /// </summary>
    public FinancialMonthlyEntryStatus Status =>
        Entries.All(e => e.IsPaid) ? FinancialMonthlyEntryStatus.Paid
        : Entries.All(e => e.IsPending) ? FinancialMonthlyEntryStatus.Pending
        : FinancialMonthlyEntryStatus.PartiallyPaid;

    public bool IsPaid => Status == FinancialMonthlyEntryStatus.Paid;
    public bool IsPartiallyPaid => Status == FinancialMonthlyEntryStatus.PartiallyPaid;
    public bool IsPending => Status == FinancialMonthlyEntryStatus.Pending;
    public bool HasDueDate => DueDate is not null;
    public bool HasPaymentAccount => PaymentAccount is not null;
    public bool IsDueToday => DueDate?.Date == DateTime.Today && !IsPaid;

    /// <summary>
    /// Groups the month's outcomes paid with a credit card into one bill per card, in the order the cards first appear.
    /// </summary>
    public static IReadOnlyList<CreditCardBill> GroupFrom(IEnumerable<MonthlyEntryProjection> entries, ISet<int>? collapsedCardIds = null) =>
        [.. entries
            .Where(e => e.IsOnCreditCardBill)
            .GroupBy(e => e.PaymentSource!.Id)
            .Select(g =>
            {
                var first = g.First();
                return new CreditCardBill(
                    first.PaymentSource!,
                    first.BillPaymentAccount,
                    first.DueDate,
                    [.. g],
                    collapsedCardIds?.Contains(g.Key) != true);
            })];
}
