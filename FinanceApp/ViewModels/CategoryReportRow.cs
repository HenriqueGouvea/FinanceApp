namespace FinanceApp.ViewModels;

/// <summary>
/// One category in the category report: its total in the month and its share of all the categories' totals.
/// </summary>
/// <param name="Name">The category's name, or "Sem categoria".</param>
/// <param name="Icon">The category's emoji.</param>
/// <param name="Color">The category's color (hex), used by its slice or column.</param>
/// <param name="Amount">What the month's entries of the category add up to.</param>
/// <param name="Share">The share (0–1) of the sum of every category's total, so the chart's parts add up to a whole.</param>
/// <param name="EntryCount">How many of the month's entries are in the category.</param>
public record CategoryReportRow(string Name, string Icon, string Color, decimal Amount, double Share, int EntryCount)
{
    /// <summary>
    /// The category's id, or null for "Sem categoria": keeps the row selected when the month reloads.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// The month's entries of the category, shown when the row (or its slice or column) is tapped.
    /// </summary>
    public IReadOnlyList<CategoryReportEntry> Entries { get; init; } = [];

    public string ShareLabel => Share.ToString("P0", Culture);
    public string EntryCountLabel => EntryCount == 1 ? "1 lançamento" : $"{EntryCount} lançamentos";

    private static readonly System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
}
