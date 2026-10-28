using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class AddEntryViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private readonly ICategoryService _categoryService;
    private Action<FinancialEntry>? _onSaved;
    private DateTime _targetMonth;
    private int _editingEntryId;
    private DateTime? _editingEndDate;
    private DateTime _editingMonth;
    private decimal _initialPaidAmount;
    private readonly List<PaymentItem> _deletedPayments = [];
    private int? _editingCategoryId;
    private IReadOnlyList<FinancialCategory> _allCategories = [];

    [ObservableProperty] string popupTitle = "Novo Lançamento";
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty] FinancialCategory? selectedCategory;
    [ObservableProperty] FinancialEntryType entryType = FinancialEntryType.Outcome;
    [ObservableProperty] RecurrenceType? selectedRecurrence;
    [ObservableProperty] int? totalInstallments;
    [ObservableProperty] bool isEstimated;
    [ObservableProperty] bool allowsPartialPayment;
    [ObservableProperty] decimal paidAmount;

    public bool HasRecurrence => SelectedRecurrence is not null;
    public bool IsInstallments => SelectedRecurrence == RecurrenceType.Installments;
    public decimal? InstallmentsTotal => IsInstallments && TotalInstallments is > 0 ? Amount * TotalInstallments : null;
    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public string PaidAmountLabel => IsIncome ? "Valor já recebido neste mês" : "Valor já pago neste mês";
    public string PaymentsHeader => IsIncome ? "Recebimentos neste mês" : "Pagamentos neste mês";
    public string PartialPaymentLabel => IsIncome ? "Recebimento parcial" : "Pagamento parcial";
    public string PartialPaymentHint => IsIncome ? "Recebido aos poucos durante o mês" : "Pago aos poucos durante o mês, como mercado";
    public bool HasPayments => Payments.Count > 0;
    /// <summary>
    /// The month's paid amount is only edited for entries paid little by little, or to remove payments recorded before the flag was turned off.
    /// </summary>
    public bool ShowsPaidAmount => IsEditing && (AllowsPartialPayment || HasPayments);
    public bool IsEditing => _editingEntryId != 0;
    public double StructuralFieldsOpacity => IsEditing ? 0.4 : 1.0;

    public ObservableCollection<FinancialCategory> Categories { get; } = [];

    /// <summary>
    /// Partial payments recorded in the month being edited. Deleting one only takes effect on save.
    /// </summary>
    public ObservableCollection<PaymentItem> Payments { get; } = [];
    public List<RecurrenceType> RecurrenceTypes { get; } = [RecurrenceType.OneTime, RecurrenceType.Recurrent, RecurrenceType.Installments];

    public AddEntryViewModel(IFinanceService financeService, ICategoryService categoryService)
    {
        _financeService = financeService;
        _categoryService = categoryService;
        Payments.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasPayments));
            OnPropertyChanged(nameof(ShowsPaidAmount));
        };
    }

    partial void OnSelectedRecurrenceChanged(RecurrenceType? value)
    {
        OnPropertyChanged(nameof(HasRecurrence));
        OnPropertyChanged(nameof(IsInstallments));
        OnPropertyChanged(nameof(InstallmentsTotal));
        SaveCommand.NotifyCanExecuteChanged();
        if (value != RecurrenceType.Installments)
            TotalInstallments = null;
    }

    partial void OnAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(InstallmentsTotal));
    }

    partial void OnAllowsPartialPaymentChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowsPaidAmount));
    }

    partial void OnTotalInstallmentsChanged(int? value)
    {
        OnPropertyChanged(nameof(InstallmentsTotal));
    }

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        OnPropertyChanged(nameof(IsIncome));
        OnPropertyChanged(nameof(PaidAmountLabel));
        OnPropertyChanged(nameof(PaymentsHeader));
        OnPropertyChanged(nameof(PartialPaymentLabel));
        OnPropertyChanged(nameof(PartialPaymentHint));
        FilterCategories();
    }

    public async Task PrepareAsync(DateTime targetMonth)
    {
        _editingEntryId = 0;
        _editingEndDate = null;
        _editingCategoryId = null;
        await LoadCategoriesAsync();

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        OnPropertyChanged(nameof(ShowsPaidAmount));
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        PopupTitle = "Novo Lançamento";
        Description = string.Empty;
        Amount = 0;
        SelectedCategory = null;
        EntryType = FinancialEntryType.Outcome;
        FilterCategories();
        SelectedRecurrence = null;
        TotalInstallments = null;
        IsEstimated = false;
        AllowsPartialPayment = false;
        PaidAmount = 0;
        _initialPaidAmount = 0;
        Payments.Clear();
        _deletedPayments.Clear();
    }

    /// <summary>
    /// Edits the template, plus the amount already paid in <paramref name="month"/> (defaults to the start month).
    /// </summary>
    public async Task PrepareForEditAsync(FinancialEntry entry, DateTime? month = null, decimal paidAmount = 0)
    {
        _editingEntryId = entry.Id;
        _editingEndDate = entry.EndDate;
        _editingCategoryId = entry.CategoryId;
        await LoadCategoriesAsync();

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        OnPropertyChanged(nameof(ShowsPaidAmount));
        _targetMonth = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1);
        PopupTitle = "Editar Lançamento";
        Description = entry.Description;
        Amount = entry.Amount;
        EntryType = entry.EntryType;
        FilterCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == entry.CategoryId);
        SelectedRecurrence = entry.Recurrence;
        TotalInstallments = entry.TotalInstallments;
        IsEstimated = entry.IsEstimated;
        AllowsPartialPayment = entry.AllowsPartialPayment;
        _editingMonth = month ?? _targetMonth;
        PaidAmount = paidAmount;
        _initialPaidAmount = paidAmount;

        _deletedPayments.Clear();
        Payments.Clear();
        foreach (var payment in await _financeService.GetPaymentsAsync(entry.Id, _editingMonth.Year, _editingMonth.Month))
            Payments.Add(PaymentItem.From(payment));
    }

    [RelayCommand]
    void DeletePayment(PaymentItem payment)
    {
        Payments.Remove(payment);
        _deletedPayments.Add(payment);
        PaidAmount = Math.Max(0, PaidAmount - payment.Amount);
    }

    [RelayCommand]
    void SelectIncome() => EntryType = FinancialEntryType.Income;

    [RelayCommand]
    void SelectOutcome() => EntryType = FinancialEntryType.Outcome;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedRecurrence is not { } recurrence)
            return;

        var entry = new FinancialEntry
        {
            Id = _editingEntryId,
            Description = Description,
            Amount = Amount,
            CategoryId = SelectedCategory?.Id,
            StartDate = new DateTime(_targetMonth.Year, _targetMonth.Month, 1),
            EndDate = _editingEndDate,
            EntryType = EntryType,
            Recurrence = recurrence,
            TotalInstallments = TotalInstallments,
            IsEstimated = IsEstimated,
            AllowsPartialPayment = AllowsPartialPayment
        };

        await _financeService.SaveEntryAsync(entry);

        if (IsEditing)
        {
            foreach (var payment in _deletedPayments)
                await _financeService.DeletePaymentAsync(payment.Id);

            // Deleting payments already lowers the month's paid amount; only a further manual change is saved on top.
            var paidAfterDeletes = Math.Max(0, _initialPaidAmount - _deletedPayments.Sum(p => p.Amount));
            if (PaidAmount != paidAfterDeletes)
                await _financeService.SetPaidAmountAsync(entry.Id, _editingMonth.Year, _editingMonth.Month, PaidAmount);
        }

        _onSaved?.Invoke(entry);
    }

    private bool CanSave() => SelectedRecurrence is not null;

    public void SetSaveCallback(Action<FinancialEntry> onSaved)
    {
        _onSaved = onSaved;
    }

    private async Task LoadCategoriesAsync()
    {
        _allCategories = await _categoryService.GetAllAsync();
    }

    // The edited entry keeps its category even when it belongs to the other type (data from before the split).
    private void FilterCategories()
    {
        var selectedId = SelectedCategory?.Id;

        Categories.Clear();
        foreach (var category in _allCategories.Where(c => c.EntryType == EntryType || c.Id == _editingCategoryId))
            Categories.Add(category);

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }
}
