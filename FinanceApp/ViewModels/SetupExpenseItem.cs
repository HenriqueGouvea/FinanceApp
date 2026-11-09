using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinanceApp.ViewModels;

/// <summary>
/// A common house expense offered by the setup wizard. Only the selected ones with an amount become entries.
/// Only the expenses whose amount can vary offer "Valor estimado", and only those paid little by little offer
/// "Pagamento parcial"; some come with them ticked, and the user can change both. An expense that
/// <see cref="OffersYearlyPayment"/> (the car insurance) can also be paid once a year or in installments every year.
/// </summary>
public partial class SetupExpenseItem : ObservableObject
{
    /// <summary>
    /// <see cref="YearlyInstallments"/> of an expense paid every month instead of every year.
    /// </summary>
    public const int MonthlyPayment = 0;

    public string Icon { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public bool CanBeEstimated { get; init; }
    public bool CanBePartiallyPaid { get; init; }
    public bool OffersYearlyPayment { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEstimatedOption), nameof(ShowsPartialPaymentOption))]
    bool isSelected;

    [ObservableProperty] decimal amount;
    [ObservableProperty] int dueDay = SetupViewModel.NoDueDay;
    [ObservableProperty] bool isEstimated;
    [ObservableProperty] bool allowsPartialPayment;

    /// <summary>
    /// <see cref="MonthlyPayment"/>, 1 for a single payment a year ("À vista") or the installments each year.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaidYearly), nameof(AmountCaption))]
    int yearlyInstallments = MonthlyPayment;

    /// <summary>
    /// The month (1–12) of the first yearly installment.
    /// </summary>
    [ObservableProperty] int yearlyStartMonth = 1;

    public bool IsPaidYearly => OffersYearlyPayment && YearlyInstallments > MonthlyPayment;
    public bool ShowsEstimatedOption => IsSelected && CanBeEstimated;
    public bool ShowsPartialPaymentOption => IsSelected && CanBePartiallyPaid;

    public string AmountCaption => !IsPaidYearly ? "Valor mensal" : YearlyInstallments == 1 ? "Valor" : "Valor de cada parcela";

    [RelayCommand]
    void ToggleEstimated()
    {
        if (CanBeEstimated)
            IsEstimated = !IsEstimated;
    }

    [RelayCommand]
    void TogglePartialPayment()
    {
        if (CanBePartiallyPaid)
            AllowsPartialPayment = !AllowsPartialPayment;
    }
}
