using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class PaymentSourceEditViewModel : ObservableObject
{
    private readonly IPaymentSourceService _paymentSourceService;
    private Func<Task>? _onSaved;
    private int _editingSourceId;

    [ObservableProperty] string popupTitle = "Nova Conta ou Cartão de Crédito";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreditCard))]
    PaymentSourceType type = PaymentSourceType.BankAccount;

    [ObservableProperty] decimal balance;

    /// <summary>
    /// Day of the month the card's bill falls due, or <see cref="AddEntryViewModel.NoDueDay"/>.
    /// </summary>
    [ObservableProperty] int billDueDay = AddEntryViewModel.NoDueDay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBillPaymentAccount))]
    PaymentSource? billPaymentAccount;

    public bool IsCreditCard => Type == PaymentSourceType.CreditCard;
    public bool HasBillPaymentAccounts => BillPaymentAccounts.Count > 0;
    public bool HasBillPaymentAccount => BillPaymentAccount is not null;

    public IReadOnlyList<PaymentSourceType> Types { get; } =
        [PaymentSourceType.BankAccount, PaymentSourceType.CreditCard, PaymentSourceType.MealVoucher];

    public List<int> BillDueDays { get; } = [AddEntryViewModel.NoDueDay, .. Enumerable.Range(1, 31)];

    /// <summary>
    /// The bank accounts that can pay a credit card's bill.
    /// </summary>
    public ObservableCollection<PaymentSource> BillPaymentAccounts { get; } = [];

    public PaymentSourceEditViewModel(IPaymentSourceService paymentSourceService)
    {
        _paymentSourceService = paymentSourceService;
        BillPaymentAccounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBillPaymentAccounts));
    }

    public async Task PrepareAsync()
    {
        _editingSourceId = 0;
        PopupTitle = "Nova Conta ou Cartão de Crédito";
        Name = string.Empty;
        Type = PaymentSourceType.BankAccount;
        Balance = 0;
        BillDueDay = AddEntryViewModel.NoDueDay;
        await LoadBillPaymentAccountsAsync();
        BillPaymentAccount = null;
    }

    public async Task PrepareForEditAsync(PaymentSource source)
    {
        _editingSourceId = source.Id;
        PopupTitle = "Editar Conta ou Cartão de Crédito";
        Name = source.Name;
        Type = source.Type;
        Balance = source.Balance;
        BillDueDay = source.BillDueDay ?? AddEntryViewModel.NoDueDay;
        await LoadBillPaymentAccountsAsync();
        BillPaymentAccount = BillPaymentAccounts.FirstOrDefault(a => a.Id == source.BillPaymentSourceId);
    }

    public void SetSaveCallback(Func<Task> onSaved)
    {
        _onSaved = onSaved;
    }

    [RelayCommand]
    void SelectType(PaymentSourceType value) => Type = value;

    [RelayCommand]
    void ClearBillPaymentAccount() => BillPaymentAccount = null;

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _paymentSourceService.SaveAsync(new PaymentSource
        {
            Id = _editingSourceId,
            Name = Name.Trim(),
            Type = Type,
            Balance = Balance,
            BillDueDay = IsCreditCard && BillDueDay != AddEntryViewModel.NoDueDay ? BillDueDay : null,
            BillPaymentSourceId = IsCreditCard ? BillPaymentAccount?.Id : null
        });

        if (_onSaved is not null)
            await _onSaved();
    }

    private async Task LoadBillPaymentAccountsAsync()
    {
        BillPaymentAccounts.Clear();
        foreach (var account in await _paymentSourceService.GetAllAsync())
        {
            if (account.Type == PaymentSourceType.BankAccount && account.Id != _editingSourceId)
                BillPaymentAccounts.Add(account);
        }
    }
}
