using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// A common house expense offered by the setup wizard. Only the selected ones with an amount become entries.
/// Only the expenses whose amount can vary offer "Valor estimado", and only those paid little by little offer
/// "Pagamento parcial"; some come with them ticked, and the user can change both. An expense that
/// <see cref="OffersYearlyPayment"/> (the car insurance) can also be paid once a year or in installments every year.
/// Paid with a credit card, the expense goes to the card's bill: it falls due with it and is never estimated nor paid
/// little by little, so those fields are hidden. A ticked expense that looks like a card purchase already added
/// says so (<see cref="PurchaseHint"/>), so the user doesn't save it twice.
/// </summary>
public partial class SetupExpenseItem : ObservableObject
{
    /// <summary>
    /// <see cref="YearlyInstallments"/> of an expense paid every month instead of every year.
    /// </summary>
    public const int MonthlyPayment = 0;

    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// The expense's name, and what the entry is called unless the user types another one (<see cref="IsDescriptionEditable"/>).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;
    public bool CanBeEstimated { get; init; }
    public bool CanBePartiallyPaid { get; init; }
    public bool OffersYearlyPayment { get; init; }

    /// <summary>
    /// A generic expense (a child's course) whose entry is better named by the user, e.g. "Inglês".
    /// </summary>
    public bool IsDescriptionEditable { get; init; }

    /// <summary>
    /// A row the user added (one more activity), which can be taken out again.
    /// </summary>
    public bool IsRemovable { get; init; }

    /// <summary>
    /// The entry an earlier run of the wizard created from this row, updated on saving (or removed once unticked).
    /// </summary>
    public int? ExistingEntryId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEstimatedOption), nameof(ShowsPartialPaymentOption), nameof(ShowsPurchaseHint))]
    bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntryDescription))]
    string customDescription = string.Empty;

    /// <summary>
    /// The description of a card purchase already added that looks like this expense, so it could be saved twice.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsPurchaseHint), nameof(PurchaseHint))]
    string? matchingPurchase;

    /// <summary>
    /// The purchases added on the credit cards step, shared by every row.
    /// </summary>
    public IList<SetupPurchaseItem> Purchases { get; set; } = [];

    /// <summary>
    /// What the entry is called: the typed description, or the expense's name when none was typed.
    /// </summary>
    public string EntryDescription =>
        IsDescriptionEditable && !string.IsNullOrWhiteSpace(CustomDescription) ? CustomDescription.Trim() : Description;

    public bool ShowsPurchaseHint => IsSelected && MatchingPurchase is not null;
    public string PurchaseHint => $"Você já incluiu \"{MatchingPurchase}\" nas compras do cartão.";

    /// <summary>
    /// The row itself while it is ticked, and empty otherwise: the amount, due day and "Pago com" fields are bound to
    /// it, so their pickers are only built for the ticked rows and the wizard opens light.
    /// </summary>
    public ObservableCollection<SetupExpenseItem> Fields { get; } = [];

    /// <summary>
    /// The accounts, vouchers and credit cards offered in "Pago com", shared by every row.
    /// </summary>
    public IList<PaymentSource> Sources { get; set; } = [];

    [ObservableProperty] decimal amount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOnCreditCardBill), nameof(ShowsDueDay), nameof(ShowsEstimatedOption), nameof(ShowsPartialPaymentOption))]
    PaymentSource? paymentSource;

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
    public bool IsOnCreditCardBill => PaymentSource?.Type == PaymentSourceType.CreditCard;
    public bool ShowsDueDay => !IsOnCreditCardBill;
    public bool ShowsEstimatedOption => IsSelected && CanBeEstimated && !IsOnCreditCardBill;
    public bool ShowsPartialPaymentOption => IsSelected && CanBePartiallyPaid && !IsOnCreditCardBill;

    public string AmountCaption => !IsPaidYearly ? "Valor mensal" : YearlyInstallments == 1 ? "Valor" : "Valor de cada parcela";

    public void RefreshMatchingPurchase() =>
        MatchingPurchase = Purchases.FirstOrDefault(p => DescriptionMatcher.IsPartOf(p.Description, EntryDescription))?.Description;

    partial void OnCustomDescriptionChanged(string value) => RefreshMatchingPurchase();

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
            Fields.Add(this);
        else
            Fields.Clear();
    }

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
