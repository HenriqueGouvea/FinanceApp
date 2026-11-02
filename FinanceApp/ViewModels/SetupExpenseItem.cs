using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinanceApp.ViewModels;

/// <summary>
/// A common house expense offered by the setup wizard. Only the selected ones with an amount become entries.
/// Varying bills come flagged as estimated and the supermarket as paid little by little, and the user can change both.
/// </summary>
public partial class SetupExpenseItem : ObservableObject
{
    public string Icon { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;

    [ObservableProperty] bool isSelected;
    [ObservableProperty] decimal amount;
    [ObservableProperty] int dueDay = SetupViewModel.NoDueDay;
    [ObservableProperty] bool isEstimated;
    [ObservableProperty] bool allowsPartialPayment;

    [RelayCommand]
    void ToggleEstimated() => IsEstimated = !IsEstimated;

    [RelayCommand]
    void TogglePartialPayment() => AllowsPartialPayment = !AllowsPartialPayment;
}
