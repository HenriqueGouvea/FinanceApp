using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FinanceApp.ViewModels;

public partial class AddEntryViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private readonly ICategoryService _categoryService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IWriteGuard _writeGuard;
    private readonly IAlertService _alertService;
    private Func<FinancialEntry, Task>? _onSaved;
    private DateTime _targetMonth;
    private int _editingEntryId;
    private DateTime? _editingEndDate;
    private DateTime _editingMonth;
    private decimal _editingTemplateAmount;
    private decimal _editingMonthAmount;
    private decimal _initialPaidAmount;
    private readonly List<PaymentItem> _deletedPayments = [];
    private IReadOnlyList<int> _editingCategoryIds = [];
    private readonly List<int> _selectedCategoryIds = [];
    private IReadOnlyList<FinancialCategory> _allCategories = [];

    [ObservableProperty] string pageTitle = "Novo Lançamento";
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOnCreditCardBill))]
    [NotifyPropertyChangedFor(nameof(CreditCardBillDueHint))]
    [NotifyPropertyChangedFor(nameof(ShowsPaidAmount))]
    [NotifyPropertyChangedFor(nameof(ShowsCreditCardChoiceHint))]
    PaymentSource? selectedPaymentSource;
    [ObservableProperty] FinancialEntryType entryType = FinancialEntryType.Outcome;
    [ObservableProperty] RecurrenceType? selectedRecurrence;
    [ObservableProperty] int? totalInstallments;
    /// <summary>
    /// For a new purchase in installments, the one that falls in the month it is added in (1 when empty): the
    /// purchase started that many months earlier.
    /// </summary>
    [ObservableProperty] int? currentInstallment;
    [ObservableProperty] bool isEstimated;
    [ObservableProperty] bool allowsPartialPayment;
    [ObservableProperty] decimal paidAmount;
    /// <summary>
    /// Day of the month the entry falls due, or <see cref="NoDueDay"/>.
    /// </summary>
    [ObservableProperty] int selectedDueDay = NoDueDay;

    public const int NoDueDay = 0;

    public const string ThisMonthOption = "Somente este mês";
    public const string ThisAndFutureOption = "Este e os próximos meses";
    public const string AllMonthsOption = "Todos os meses";
    public const string CancelOption = "Cancelar";

    public bool HasRecurrence => SelectedRecurrence is not null;
    public bool IsInstallments => SelectedRecurrence == RecurrenceType.Installments;
    /// <summary>
    /// "Parcela atual" is only asked when adding a purchase in installments: an edited one keeps its months.
    /// </summary>
    public bool ShowsCurrentInstallment => IsInstallments && !IsEditing;
    private bool IsCurrentInstallmentValid =>
        CurrentInstallment is null || (CurrentInstallment >= 1 && (TotalInstallments is null || CurrentInstallment <= TotalInstallments));
    /// <summary>
    /// The month of the first installment of a purchase that started before the month it is added in, or null.
    /// </summary>
    public DateTime? FirstInstallmentMonth =>
        ShowsCurrentInstallment && IsCurrentInstallmentValid && CurrentInstallment > 1 ? _targetMonth.AddMonths(1 - CurrentInstallment.Value) : null;
    public string CurrentInstallmentHint =>
        !ShowsCurrentInstallment ? string.Empty
        : !IsCurrentInstallmentValid ? "A parcela atual não pode passar do nº de parcelas."
        : FirstInstallmentMonth is { } first ? $"A 1ª parcela foi em {first.ToString("MMMM 'de' yyyy", CultureInfo.CurrentCulture)}."
        : "A parcela que cai neste mês. Deixe vazio se a compra começa agora.";
    public bool IsAnnual => SelectedRecurrence == RecurrenceType.Annual;
    /// <summary>
    /// The installment purchase's total, or the yearly total of an annual entry paid in installments.
    /// </summary>
    public decimal? InstallmentsTotal =>
        (IsInstallments && TotalInstallments is > 0) || (IsAnnual && TotalInstallments is > 1) ? Amount * TotalInstallments : null;
    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public string PaidAmountLabel => IsIncome ? "Valor já recebido neste mês" : "Valor já pago neste mês";
    public string PaymentsHeader => IsIncome ? "Recebimentos neste mês" : "Pagamentos neste mês";
    public string PartialPaymentLabel => IsIncome ? "Recebimento parcial" : "Pagamento parcial";
    public string PartialPaymentHint => IsIncome ? "Recebido aos poucos durante o mês" : "Pago aos poucos durante o mês, como mercado";
    public string PaymentSourceLabel => IsIncome ? "Recebido em" : "Pago com";
    /// <summary>
    /// "Pago com"/"Recebido em" is hidden while the user has no account, card or voucher to pick.
    /// </summary>
    public bool HasPaymentSources => PaymentSources.Count > 0;
    /// <summary>
    /// The note on what a credit card changes only makes sense for an outcome, when a card can be picked and isn't yet.
    /// </summary>
    public bool ShowsCreditCardChoiceHint => !IsIncome && !IsOnCreditCardBill
        && PaymentSources.Any(s => s.Type == PaymentSourceType.CreditCard);
    /// <summary>
    /// An outcome paid with a credit card falls due with the card's bill, so it has no due day of its own.
    /// It is also neither estimated nor paid little by little: a card purchase has a known amount, paid with the bill.
    /// </summary>
    public bool IsOnCreditCardBill => !IsIncome && SelectedPaymentSource?.Type == PaymentSourceType.CreditCard;
    public string CreditCardBillDueHint => SelectedPaymentSource?.BillDueDay is { } day
        ? $"Vence com a fatura do cartão de crédito (dia {day})"
        : "Vence com a fatura do cartão de crédito";
    public bool HasPayments => Payments.Count > 0;
    /// <summary>
    /// The month's paid amount is only edited for entries paid little by little, or to remove payments recorded before the flag was turned off
    /// (or before the entry moved to a credit card).
    /// </summary>
    public bool ShowsPaidAmount => IsEditing && ((AllowsPartialPayment && !IsOnCreditCardBill) || HasPayments);
    public bool IsEditing => _editingEntryId != 0;
    public double StructuralFieldsOpacity => IsEditing ? 0.4 : 1.0;

    public ObservableCollection<CategoryChip> Categories { get; } = [];

    /// <summary>
    /// The picked categories in the order they were picked: the first is the main one.
    /// </summary>
    public IReadOnlyList<int> SelectedCategoryIds => _selectedCategoryIds;

    /// <summary>
    /// With several categories picked, says which one is the main: the first picked, shown as the row's icon.
    /// </summary>
    public string MainCategoryHint => ShowsMainCategoryHint && Categories.FirstOrDefault(c => c.IsMain)?.Category is { } main
        ? $"Principal: {main.Icon} {main.Name}, a primeira escolhida. Ela é o ícone do lançamento."
        : string.Empty;

    public bool ShowsMainCategoryHint => _selectedCategoryIds.Count > 1;

    public ObservableCollection<PaymentSource> PaymentSources { get; } = [];

    /// <summary>
    /// Partial payments recorded in the month being edited. Deleting one only takes effect on save.
    /// </summary>
    public ObservableCollection<PaymentItem> Payments { get; } = [];
    public List<RecurrenceType> RecurrenceTypes { get; } =
        [RecurrenceType.OneTime, RecurrenceType.Recurrent, RecurrenceType.Installments, RecurrenceType.Annual];
    public List<int> DueDays { get; } = [NoDueDay, .. Enumerable.Range(1, 31)];

    public AddEntryViewModel(IFinanceService financeService, ICategoryService categoryService, IPaymentSourceService paymentSourceService, IWriteGuard writeGuard, IAlertService alertService)
    {
        _financeService = financeService;
        _categoryService = categoryService;
        _paymentSourceService = paymentSourceService;
        _writeGuard = writeGuard;
        _alertService = alertService;
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
        OnPropertyChanged(nameof(IsAnnual));
        OnPropertyChanged(nameof(InstallmentsTotal));
        if (value is not (RecurrenceType.Installments or RecurrenceType.Annual))
            TotalInstallments = null;
        if (value != RecurrenceType.Installments)
            CurrentInstallment = null;
        NotifyCurrentInstallmentChanged();
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
        NotifyCurrentInstallmentChanged();
    }

    partial void OnCurrentInstallmentChanged(int? value)
    {
        NotifyCurrentInstallmentChanged();
    }

    private void NotifyCurrentInstallmentChanged()
    {
        OnPropertyChanged(nameof(ShowsCurrentInstallment));
        OnPropertyChanged(nameof(FirstInstallmentMonth));
        OnPropertyChanged(nameof(CurrentInstallmentHint));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        OnPropertyChanged(nameof(IsIncome));
        OnPropertyChanged(nameof(PaidAmountLabel));
        OnPropertyChanged(nameof(PaymentsHeader));
        OnPropertyChanged(nameof(PartialPaymentLabel));
        OnPropertyChanged(nameof(PartialPaymentHint));
        OnPropertyChanged(nameof(PaymentSourceLabel));
        OnPropertyChanged(nameof(IsOnCreditCardBill));
        OnPropertyChanged(nameof(ShowsPaidAmount));
        OnPropertyChanged(nameof(ShowsCreditCardChoiceHint));
        FilterCategories();
    }

    public async Task PrepareAsync(DateTime targetMonth)
    {
        _editingEntryId = 0;
        _editingEndDate = null;
        _editingCategoryIds = [];
        _selectedCategoryIds.Clear();
        await LoadCategoriesAsync();
        await LoadPaymentSourcesAsync();

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        OnPropertyChanged(nameof(ShowsPaidAmount));
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        PageTitle = "Novo Lançamento";
        Description = string.Empty;
        Amount = 0;
        SelectedPaymentSource = null;
        EntryType = FinancialEntryType.Outcome;
        FilterCategories();
        SelectedRecurrence = null;
        TotalInstallments = null;
        CurrentInstallment = null;
        IsEstimated = false;
        AllowsPartialPayment = false;
        SelectedDueDay = NoDueDay;
        PaidAmount = 0;
        _initialPaidAmount = 0;
        Payments.Clear();
        _deletedPayments.Clear();
        NotifyCurrentInstallmentChanged();
    }

    /// <summary>
    /// Edits the template, plus the amount already paid in <paramref name="month"/> (defaults to the start month).
    /// The amount shown is <paramref name="monthAmount"/>, the one planned for that month (the template's by default).
    /// </summary>
    public async Task PrepareForEditAsync(FinancialEntry entry, DateTime? month = null, decimal paidAmount = 0, decimal? monthAmount = null)
    {
        _editingEntryId = entry.Id;
        _editingEndDate = entry.EndDate;
        _editingCategoryIds = await _financeService.GetEntryCategoryIdsAsync(entry.Id);
        _selectedCategoryIds.Clear();
        _selectedCategoryIds.AddRange(_editingCategoryIds);
        await LoadCategoriesAsync();
        await LoadPaymentSourcesAsync();

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        OnPropertyChanged(nameof(ShowsPaidAmount));
        _targetMonth = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1);
        PageTitle = "Editar Lançamento";
        Description = entry.Description;
        Amount = monthAmount ?? entry.Amount;
        _editingTemplateAmount = entry.Amount;
        _editingMonthAmount = Amount;
        EntryType = entry.EntryType;
        FilterCategories();
        SelectedPaymentSource = PaymentSources.FirstOrDefault(s => s.Id == entry.PaymentSourceId);
        SelectedRecurrence = entry.Recurrence;
        TotalInstallments = entry.TotalInstallments;
        CurrentInstallment = null;
        IsEstimated = entry.IsEstimated;
        AllowsPartialPayment = entry.AllowsPartialPayment;
        SelectedDueDay = entry.DueDay ?? NoDueDay;
        _editingMonth = month ?? _targetMonth;
        NotifyCurrentInstallmentChanged();
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

        // A new amount for a repeating entry applies to the months the user picks.
        AmountChangeScope? amountScope = null;
        if (IsEditing && Amount != _editingMonthAmount)
        {
            amountScope = recurrence == RecurrenceType.OneTime ? AmountChangeScope.All : await AskAmountScopeAsync();
            if (amountScope is null)
                return;
        }

        var entry = new FinancialEntry
        {
            Id = _editingEntryId,
            Description = Description,
            Amount = IsEditing && amountScope is null ? _editingTemplateAmount : Amount,
            PaymentSourceId = SelectedPaymentSource?.Id,
            StartDate = ShowsCurrentInstallment ? _targetMonth.AddMonths(1 - (CurrentInstallment ?? 1)) : _targetMonth,
            EndDate = _editingEndDate,
            EntryType = EntryType,
            Recurrence = recurrence,
            TotalInstallments = recurrence switch
            {
                RecurrenceType.Installments => TotalInstallments,
                // Within a year: one installment (a single payment) is stored as none.
                RecurrenceType.Annual when TotalInstallments is > 1 => Math.Min(TotalInstallments.Value, 12),
                _ => null
            },
            IsEstimated = IsEstimated && !IsOnCreditCardBill,
            AllowsPartialPayment = AllowsPartialPayment && !IsOnCreditCardBill,
            DueDay = SelectedDueDay == NoDueDay || IsOnCreditCardBill ? null : SelectedDueDay
        };

        var saved = await _writeGuard.TryAsync(async () =>
        {
            if (amountScope is { } scope)
                await _financeService.SaveEntryAsync(entry, [.. _selectedCategoryIds], _editingMonth.Year, _editingMonth.Month, scope);
            else
                await _financeService.SaveEntryAsync(entry, [.. _selectedCategoryIds]);

            if (IsEditing)
            {
                foreach (var payment in _deletedPayments)
                    await _financeService.DeletePaymentAsync(payment.Id);

                // Deleting payments already lowers the month's paid amount; only a further manual change is saved on top.
                var paidAfterDeletes = Math.Max(0, _initialPaidAmount - _deletedPayments.Sum(p => p.Amount));
                if (PaidAmount != paidAfterDeletes)
                    await _financeService.SetPaidAmountAsync(entry.Id, _editingMonth.Year, _editingMonth.Month, PaidAmount);
            }
        });

        if (saved && _onSaved is not null)
            await _onSaved(entry);
    }

    private bool CanSave() => SelectedRecurrence is not null && (!ShowsCurrentInstallment || IsCurrentInstallmentValid);

    private async Task<AmountChangeScope?> AskAmountScopeAsync()
    {
        var choice = await _alertService.ChooseAsync($"Alterar o valor de \"{Description}\"", CancelOption,
            ThisMonthOption, ThisAndFutureOption, AllMonthsOption);
        return choice switch
        {
            ThisMonthOption => AmountChangeScope.ThisMonth,
            ThisAndFutureOption => AmountChangeScope.ThisAndFuture,
            AllMonthsOption => AmountChangeScope.All,
            _ => null
        };
    }

    public void SetSaveCallback(Func<FinancialEntry, Task> onSaved)
    {
        _onSaved = onSaved;
    }

    private async Task LoadCategoriesAsync()
    {
        _allCategories = await _categoryService.GetAllAsync();
    }

    private async Task LoadPaymentSourcesAsync()
    {
        PaymentSources.Clear();
        foreach (var source in await _paymentSourceService.GetAllAsync())
            PaymentSources.Add(source);
        OnPropertyChanged(nameof(HasPaymentSources));
        OnPropertyChanged(nameof(ShowsCreditCardChoiceHint));
    }

    [RelayCommand]
    void ToggleCategory(CategoryChip chip)
    {
        if (!_selectedCategoryIds.Remove(chip.Category.Id))
            _selectedCategoryIds.Add(chip.Category.Id);

        UpdateCategoryChips();
    }

    // Shows the categories of the entry's type. The edited entry keeps its categories even when they belong to the
    // other type (data from before the split); picked ones of the other type are dropped when the type changes.
    private void FilterCategories()
    {
        Categories.Clear();
        foreach (var category in _allCategories.Where(c => c.EntryType == EntryType || _editingCategoryIds.Contains(c.Id)))
            Categories.Add(new CategoryChip(category));

        _selectedCategoryIds.RemoveAll(id => Categories.All(c => c.Category.Id != id));
        UpdateCategoryChips();
    }

    private void UpdateCategoryChips()
    {
        foreach (var chip in Categories)
        {
            chip.Position = _selectedCategoryIds.IndexOf(chip.Category.Id) + 1;
            chip.IsSelected = chip.Position > 0;
        }

        OnPropertyChanged(nameof(SelectedCategoryIds));
        OnPropertyChanged(nameof(MainCategoryHint));
        OnPropertyChanged(nameof(ShowsMainCategoryHint));
    }
}
