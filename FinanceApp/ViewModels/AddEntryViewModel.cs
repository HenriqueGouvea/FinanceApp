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
    private int? _editingCategoryId;
    private IReadOnlyList<FinancialCategory> _allCategories = [];

    [ObservableProperty] string popupTitle = "Novo Lançamento";
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty] FinancialCategory? selectedCategory;
    [ObservableProperty] FinancialEntryType entryType = FinancialEntryType.Outcome;
    [ObservableProperty] RecurrenceType? selectedRecurrence;
    [ObservableProperty] int? totalInstallments;

    public bool HasRecurrence => SelectedRecurrence is not null;
    public bool IsInstallments => SelectedRecurrence == RecurrenceType.Installments;
    public decimal? InstallmentsTotal => IsInstallments && TotalInstallments is > 0 ? Amount * TotalInstallments : null;
    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public bool IsEditing => _editingEntryId != 0;
    public double StructuralFieldsOpacity => IsEditing ? 0.4 : 1.0;

    public ObservableCollection<FinancialCategory> Categories { get; } = [];
    public List<RecurrenceType> RecurrenceTypes { get; } = [RecurrenceType.OneTime, RecurrenceType.Recurrent, RecurrenceType.Installments];

    public AddEntryViewModel(IFinanceService financeService, ICategoryService categoryService)
    {
        _financeService = financeService;
        _categoryService = categoryService;
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

    partial void OnTotalInstallmentsChanged(int? value)
    {
        OnPropertyChanged(nameof(InstallmentsTotal));
    }

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        OnPropertyChanged(nameof(IsIncome));
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
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        PopupTitle = "Novo Lançamento";
        Description = string.Empty;
        Amount = 0;
        SelectedCategory = null;
        EntryType = FinancialEntryType.Outcome;
        FilterCategories();
        SelectedRecurrence = null;
        TotalInstallments = null;
    }

    public async Task PrepareForEditAsync(FinancialEntry entry)
    {
        _editingEntryId = entry.Id;
        _editingEndDate = entry.EndDate;
        _editingCategoryId = entry.CategoryId;
        await LoadCategoriesAsync();

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        _targetMonth = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1);
        PopupTitle = "Editar Lançamento";
        Description = entry.Description;
        Amount = entry.Amount;
        EntryType = entry.EntryType;
        FilterCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == entry.CategoryId);
        SelectedRecurrence = entry.Recurrence;
        TotalInstallments = entry.TotalInstallments;
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
            TotalInstallments = TotalInstallments
        };

        await _financeService.SaveEntryAsync(entry);
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
