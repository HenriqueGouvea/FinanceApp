using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// Records a payment (e.g. today's supermarket bill) on top of what an entry already has paid in one month.
/// </summary>
public partial class PartialPaymentViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private Action? _onSaved;
    private int _entryId;
    private DateTime _month;

    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] FinancialEntryType entryType;
    [ObservableProperty] string paymentDescription = string.Empty;
    [ObservableProperty] DateTime paymentDate = DateTime.Today;
    [ObservableProperty] DateTime minimumDate = DateTime.Today;
    [ObservableProperty] DateTime maximumDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewPaidAmount))]
    [NotifyPropertyChangedFor(nameof(NewOverBudgetAmount))]
    [NotifyPropertyChangedFor(nameof(IsNewTotalOverBudget))]
    decimal paidAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewOverBudgetAmount))]
    [NotifyPropertyChangedFor(nameof(IsNewTotalOverBudget))]
    decimal totalAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewPaidAmount))]
    [NotifyPropertyChangedFor(nameof(NewOverBudgetAmount))]
    [NotifyPropertyChangedFor(nameof(IsNewTotalOverBudget))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    decimal amount;

    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public string Title => IsIncome ? "Receber parcial" : "Pagar parcial";
    public string PaidAmountLabel => IsIncome ? "Já recebido" : "Já gasto";
    public string AmountLabel => IsIncome ? "Valor recebido agora" : "Valor gasto agora";
    public decimal NewPaidAmount => PaidAmount + Amount;
    public decimal NewOverBudgetAmount => Math.Max(0, NewPaidAmount - TotalAmount);
    public bool IsNewTotalOverBudget => NewOverBudgetAmount > 0;

    public PartialPaymentViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        OnPropertyChanged(nameof(IsIncome));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PaidAmountLabel));
        OnPropertyChanged(nameof(AmountLabel));
    }

    /// <summary>
    /// The payment date defaults to today, kept within <paramref name="month"/> when another month is shown.
    /// </summary>
    public void Prepare(MonthlyEntryProjection projection, DateTime month, DateTime? today = null)
    {
        _entryId = projection.FinancialEntryId;
        _month = new DateTime(month.Year, month.Month, 1);
        Description = projection.Description;
        EntryType = projection.EntryType;
        PaidAmount = projection.PaidAmount;
        TotalAmount = projection.PlannedAmount ?? projection.EffectiveAmount;
        Amount = 0;
        PaymentDescription = string.Empty;
        MinimumDate = _month;
        MaximumDate = _month.AddMonths(1).AddDays(-1);
        var date = (today ?? DateTime.Today).Date;
        PaymentDate = date < MinimumDate ? MinimumDate : date > MaximumDate ? MaximumDate : date;
    }

    public void SetSaveCallback(Action onSaved)
    {
        _onSaved = onSaved;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _financeService.AddPaymentAsync(_entryId, _month.Year, _month.Month, Amount, PaymentDate, PaymentDescription.Trim());
        _onSaved?.Invoke();
    }

    private bool CanSave() => Amount > 0;
}
