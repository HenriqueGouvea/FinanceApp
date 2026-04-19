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

    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty] string selectedCategory = string.Empty;
    [ObservableProperty] FinancialEntryType entryType = FinancialEntryType.Outcome;
    [ObservableProperty] RecurrenceType selectedRecurrence = RecurrenceType.OneTime;
    [ObservableProperty] int? totalInstallments;

    public bool IsInstallments => SelectedRecurrence == RecurrenceType.Installments;

    public List<string> Categories { get; } = ["Moradia", "Educação", "Contas", "Lazer"];
    public List<RecurrenceType> RecurrenceTypes { get; } = [RecurrenceType.OneTime, RecurrenceType.Recurrent, RecurrenceType.Installments];
    public List<FinancialEntryType> EntryTypes { get; } = [FinancialEntryType.Outcome, FinancialEntryType.Income];

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

    public void Prepare(DateTime targetMonth)
    {
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        Description = string.Empty;
        Amount = 0;
        SelectedCategory = string.Empty;
        EntryType = FinancialEntryType.Outcome;
        SelectedRecurrence = RecurrenceType.OneTime;
        TotalInstallments = null;
    }

    [RelayCommand]
    private async Task Save()
    {
        var entry = new FinancialEntry
        {
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
