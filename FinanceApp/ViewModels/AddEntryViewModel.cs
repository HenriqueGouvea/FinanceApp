using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class AddEntryViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private Action<FinancialEntry>? _onSaved;
    private DateTime _targetMonth;
    private int _editingEntryId;

    [ObservableProperty] string popupTitle = "Novo Lançamento";
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty] string selectedCategory = string.Empty;
    [ObservableProperty] FinancialEntryType entryType = FinancialEntryType.Outcome;
    [ObservableProperty] RecurrenceType selectedRecurrence = RecurrenceType.OneTime;
    [ObservableProperty] int? totalInstallments;

    public bool IsInstallments => SelectedRecurrence == RecurrenceType.Installments;
    public bool IsIncome => EntryType == FinancialEntryType.Income;
    public bool IsEditing => _editingEntryId != 0;
    public double StructuralFieldsOpacity => IsEditing ? 0.4 : 1.0;

    public List<string> Categories { get; } = ["Moradia", "Educação", "Contas", "Lazer"];
    public List<RecurrenceType> RecurrenceTypes { get; } = [RecurrenceType.OneTime, RecurrenceType.Recurrent, RecurrenceType.Installments];

    public AddEntryViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    partial void OnSelectedRecurrenceChanged(RecurrenceType value)
    {
        OnPropertyChanged(nameof(IsInstallments));
        if (value != RecurrenceType.Installments)
            TotalInstallments = null;
    }

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        OnPropertyChanged(nameof(IsIncome));
    }

    public void Prepare(DateTime targetMonth)
    {
        _editingEntryId = 0;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        PopupTitle = "Novo Lançamento";
        Description = string.Empty;
        Amount = 0;
        SelectedCategory = string.Empty;
        EntryType = FinancialEntryType.Outcome;
        SelectedRecurrence = RecurrenceType.OneTime;
        TotalInstallments = null;
    }

    public void PrepareForEdit(FinancialEntry entry)
    {
        _editingEntryId = entry.Id;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(StructuralFieldsOpacity));
        _targetMonth = new DateTime(entry.StartDate.Year, entry.StartDate.Month, 1);
        PopupTitle = "Editar Lançamento";
        Description = entry.Description;
        Amount = entry.Amount;
        SelectedCategory = entry.Category;
        EntryType = entry.EntryType;
        SelectedRecurrence = entry.Recurrence;
        TotalInstallments = entry.TotalInstallments;
    }

    [RelayCommand]
    void SelectIncome() => EntryType = FinancialEntryType.Income;

    [RelayCommand]
    void SelectOutcome() => EntryType = FinancialEntryType.Outcome;

    [RelayCommand]
    private async Task Save()
    {
        var entry = new FinancialEntry
        {
            Id = _editingEntryId,
            Description = Description,
            Amount = Amount,
            Category = SelectedCategory,
            StartDate = new DateTime(_targetMonth.Year, _targetMonth.Month, 1),
            EntryType = EntryType,
            Recurrence = SelectedRecurrence,
            TotalInstallments = TotalInstallments
        };

        await _financeService.SaveEntryAsync(entry);
        _onSaved?.Invoke(entry);
    }

    public void SetSaveCallback(Action<FinancialEntry> onSaved)
    {
        _onSaved = onSaved;
    }
}
