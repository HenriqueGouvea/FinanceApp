namespace FinanceApp.Models;

/// <summary>
/// Links an entry to one of its categories. An entry can have several (e.g. "Farmácia" in "Saúde" and "Filhos").
/// </summary>
public class FinancialCategoryEntry
{
    public int Id { get; set; }
    public int FinancialEntryId { get; set; }
    public int FinancialCategoryId { get; set; }

    /// <summary>
    /// The order the categories were picked in: the first one (0) is the main category, shown as the row's icon.
    /// </summary>
    public int Position { get; set; }
}
