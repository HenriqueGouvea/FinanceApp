using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// What a month shows, built once from its entries: the rows of each list (tinted every other row) and the totals.
/// It holds no UI state, so it can be built off the UI thread and swapped into <see cref="MonthViewModel"/> at once.
/// </summary>
public sealed class MonthContent
{
    public static readonly MonthContent Empty = Build([]);

    public IReadOnlyList<MonthlyEntryProjection> Entries { get; private init; } = [];

    /// <summary>
    /// The outcomes listed on their own: those paid with a credit card are listed under the card's bill instead.
    /// </summary>
    public IReadOnlyList<MonthlyEntryProjection> OutcomeEntries { get; private init; } = [];

    public IReadOnlyList<MonthlyEntryProjection> IncomeEntries { get; private init; } = [];

    /// <summary>
    /// One bill per credit card used this month, all expanded. Their entries are already counted in the outcome totals.
    /// </summary>
    public IReadOnlyList<CreditCardBill> CreditCardBills { get; private init; } = [];

    public decimal TotalIncomes { get; private init; }
    public decimal TotalOutcomes { get; private init; }
    public decimal ReceivedIncomes { get; private init; }
    public decimal PendingIncomes { get; private init; }
    public decimal PaidOutcomes { get; private init; }
    public decimal PendingOutcomes { get; private init; }
    public int OutcomeCount { get; private init; }
    public int IncomeCount => IncomeEntries.Count;

    public static MonthContent Build(IEnumerable<MonthlyEntryProjection> entries)
    {
        List<MonthlyEntryProjection> all = [.. entries];
        var outcomes = all.Where(e => e.EntryType == FinancialEntryType.Outcome).ToList();
        IReadOnlyList<MonthlyEntryProjection> incomes = WithAlternatingRows(all.Where(e => e.EntryType == FinancialEntryType.Income));

        return new MonthContent
        {
            Entries = all,
            OutcomeEntries = WithAlternatingRows(outcomes.Where(e => !e.IsOnCreditCardBill)),
            IncomeEntries = incomes,
            CreditCardBills = [.. CreditCardBill.GroupFrom(all).Select(bill => bill with { Entries = WithAlternatingRows(bill.Entries) })],
            TotalIncomes = incomes.Sum(e => e.EffectiveAmount),
            TotalOutcomes = outcomes.Sum(e => e.EffectiveAmount),
            ReceivedIncomes = incomes.Sum(e => e.PaidAmount),
            PendingIncomes = incomes.Sum(e => e.PendingAmount),
            PaidOutcomes = outcomes.Sum(e => e.PaidAmount),
            PendingOutcomes = outcomes.Sum(e => e.PendingAmount),
            OutcomeCount = outcomes.Count
        };
    }

    /// <summary>
    /// Whether two bill lists show the same rows. Bills are records, but their entry lists compare by reference.
    /// </summary>
    public static bool SameBills(IReadOnlyList<CreditCardBill> first, IReadOnlyList<CreditCardBill> second) =>
        first.Count == second.Count
        && first.Zip(second).All(pair => WithoutEntries(pair.First) == WithoutEntries(pair.Second)
                                         && pair.First.Entries.SequenceEqual(pair.Second.Entries));

    // The same empty list on both sides, so the record comparison skips the entries.
    private static CreditCardBill WithoutEntries(CreditCardBill bill) => bill with { Entries = Array.Empty<MonthlyEntryProjection>() };

    private static List<MonthlyEntryProjection> WithAlternatingRows(IEnumerable<MonthlyEntryProjection> entries) =>
        [.. entries.Select((entry, index) => entry with { IsAlternateRow = index % 2 == 1 })];
}
