using CommunityToolkit.Mvvm.ComponentModel;
using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A category on the entry page, picked or not by tapping it. The first one picked is the entry's main category.
/// </summary>
public partial class CategoryChip : ObservableObject
{
    public CategoryChip(FinancialCategory category)
    {
        Category = category;
    }

    public FinancialCategory Category { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMain))]
    bool isSelected;

    /// <summary>
    /// 1 for the main category, 2 for the next one picked..., 0 when not picked.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMain))]
    int position;

    public bool IsMain => IsSelected && Position == 1;
}
