namespace FinanceApp.Models;

/// <summary>
/// How much a month's incomes or outcomes of one category add up to (non-persisted, for the category report).
/// An entry with several categories counts in each one.
/// </summary>
/// <param name="EntryType">Whether the total is of the month's incomes or outcomes.</param>
/// <param name="Category">The category, or null for the uncategorized entries.</param>
/// <param name="Amount">The sum of the month's amounts of its entries.</param>
/// <param name="EntryCount">How many of the month's entries are in the category.</param>
public record CategoryTotal(FinancialEntryType EntryType, FinancialCategory? Category, decimal Amount, int EntryCount)
{
    /// <summary>
    /// The month's entries of the category, the largest first, listed when the user taps the category in the report.
    /// Left out of the record's equality, which compares the totals.
    /// </summary>
    public IReadOnlyList<MonthlyEntryProjection> Entries { get; init; } = [];

    public virtual bool Equals(CategoryTotal? other) =>
        other is not null && EntryType == other.EntryType && Equals(Category, other.Category)
        && Amount == other.Amount && EntryCount == other.EntryCount;

    public override int GetHashCode() => HashCode.Combine(EntryType, Category, Amount, EntryCount);
}
