using FinanceApp.Models;

namespace FinanceApp.ViewModels;

public class CategoryGroup(FinancialEntryType entryType, string title, IEnumerable<FinancialCategory> categories)
    : List<FinancialCategory>(categories)
{
    public FinancialEntryType EntryType { get; } = entryType;
    public string Title { get; } = title;
}
