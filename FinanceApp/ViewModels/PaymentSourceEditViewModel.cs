using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class PaymentSourceEditViewModel : ObservableObject
{
    private readonly IPaymentSourceService _paymentSourceService;
    private Func<Task>? _onSaved;
    private int _editingSourceId;

    [ObservableProperty] string popupTitle = "Nova Conta ou Cartão";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    string name = string.Empty;

    [ObservableProperty] PaymentSourceType type = PaymentSourceType.BankAccount;
    [ObservableProperty] decimal balance;

    public IReadOnlyList<PaymentSourceType> Types { get; } =
        [PaymentSourceType.BankAccount, PaymentSourceType.CreditCard, PaymentSourceType.MealVoucher];

    public PaymentSourceEditViewModel(IPaymentSourceService paymentSourceService)
    {
        _paymentSourceService = paymentSourceService;
    }

    public void Prepare()
    {
        _editingSourceId = 0;
        PopupTitle = "Nova Conta ou Cartão";
        Name = string.Empty;
        Type = PaymentSourceType.BankAccount;
        Balance = 0;
    }

    public void PrepareForEdit(PaymentSource source)
    {
        _editingSourceId = source.Id;
        PopupTitle = "Editar Conta ou Cartão";
        Name = source.Name;
        Type = source.Type;
        Balance = source.Balance;
    }

    public void SetSaveCallback(Func<Task> onSaved)
    {
        _onSaved = onSaved;
    }

    [RelayCommand]
    void SelectType(PaymentSourceType value) => Type = value;

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _paymentSourceService.SaveAsync(new PaymentSource
        {
            Id = _editingSourceId,
            Name = Name.Trim(),
            Type = Type,
            Balance = Balance
        });

        if (_onSaved is not null)
            await _onSaved();
    }
}
