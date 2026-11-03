namespace FinanceApp.Models;

/// <summary>
/// Non-persisted read model: a payment source with what is currently left on it.
/// Account and voucher balances are only what the user types; a credit card's available limit is its limit
/// minus what is in use (<see cref="UsedLimit"/>, see <c>IFinanceService.GetUsedCreditLimitsAsync</c>).
/// </summary>
public record PaymentSourceSummary(PaymentSource Source, decimal UsedLimit = 0)
{
    public bool IsCreditCard => Source.Type == PaymentSourceType.CreditCard;

    public decimal AvailableLimit => Source.Balance - UsedLimit;

    public bool IsOverLimit => IsCreditCard && AvailableLimit < 0;

    /// <summary>
    /// The account or voucher balance, or the limit still available on a credit card.
    /// </summary>
    public decimal CurrentBalance => IsCreditCard ? AvailableLimit : Source.Balance;

    public static IReadOnlyList<PaymentSourceSummary> Build(
        IEnumerable<PaymentSource> sources, IReadOnlyDictionary<int, decimal> usedLimits) =>
        [.. sources.Select(s => new PaymentSourceSummary(s, usedLimits.GetValueOrDefault(s.Id)))];
}
