using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// Adds a payment (e.g. today's supermarket bill) to what an entry already has paid in one month.
/// </summary>
public partial class PartialPaymentViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private Action? _onSaved;
    private int _entryId;
    private DateTime _month;

    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] FinancialEntryType entryType;
    [ObservableProperty] decimal paidAmount;
    [ObservableProperty] decimal totalAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewPaidAmount))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    decimal amount;

    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public string Title => IsIncome ? "Receber parcial" : "Pagar parcial";
    public string PaidAmountLabel => IsIncome ? "Já recebido" : "Já gasto";
    public string AmountLabel => IsIncome ? "Valor recebido agora" : "Valor gasto agora";
    public decimal NewPaidAmount => PaidAmount + Amount;

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

    partial void OnPaidAmountChanged(decimal value) => OnPropertyChanged(nameof(NewPaidAmount));

    public void Prepare(MonthlyEntryProjection projection, DateTime month)
    {
        _entryId = projection.FinancialEntryId;
        _month = new DateTime(month.Year, month.Month, 1);
        Description = projection.Description;
        EntryType = projection.EntryType;
        PaidAmount = projection.PaidAmount;
        TotalAmount = projection.EffectiveAmount;
        Amount = 0;
    }

    public void SetSaveCallback(Action onSaved)
    {
        _onSaved = onSaved;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _financeService.AddPaidAmountAsync(_entryId, _month.Year, _month.Month, Amount);
        _onSaved?.Invoke();
    }

    private bool CanSave() => Amount > 0;
}
