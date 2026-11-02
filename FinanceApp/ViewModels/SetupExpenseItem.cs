using CommunityToolkit.Mvvm.ComponentModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// A common house expense offered by the setup wizard. Only the selected ones with an amount become entries.
/// </summary>
public partial class SetupExpenseItem : ObservableObject
{
    public string Icon { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public bool IsEstimated { get; init; }
    public bool AllowsPartialPayment { get; init; }

    /// <summary>
    /// Expenses paid little by little (e.g. supermarket) have no due date.
    /// </summary>
    public bool HasDueDay => !AllowsPartialPayment;
    public bool HasHint => Hint.Length > 0;

    [ObservableProperty] bool isSelected;
    [ObservableProperty] decimal amount;
    [ObservableProperty] int dueDay = SetupViewModel.NoDueDay;
}
