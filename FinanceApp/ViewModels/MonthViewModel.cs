using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    public ObservableCollection<MonthlyEntryProjection> Entries { get; } = [];

    [ObservableProperty]
    string monthName = string.Empty;

    [ObservableProperty]
    bool isLoaded;

    [ObservableProperty]
    bool isOutcomesExpanded = true;

    [ObservableProperty]
    bool isIncomesExpanded = true;

    public DateTime Date { get; set; }

    public IEnumerable<MonthlyEntryProjection> OutcomeEntries =>
        Entries.Where(e => e.EntryType == FinancialEntryType.Outcome);

    public IEnumerable<MonthlyEntryProjection> IncomeEntries =>
        Entries.Where(e => e.EntryType == FinancialEntryType.Income);

    public decimal TotalIncomes => Entries
        .Where(e => e.EntryType == FinancialEntryType.Income)
        .Sum(e => e.EffectiveAmount);

    public decimal TotalOutcomes => Entries
        .Where(e => e.EntryType == FinancialEntryType.Outcome)
        .Sum(e => e.EffectiveAmount);

    public decimal Balance => TotalIncomes - TotalOutcomes;

    public int OutcomeCount => OutcomeEntries.Count();
    public int IncomeCount => IncomeEntries.Count();

    public MonthViewModel()
    {
        Entries.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TotalIncomes));
            OnPropertyChanged(nameof(TotalOutcomes));
            OnPropertyChanged(nameof(Balance));
            OnPropertyChanged(nameof(OutcomeEntries));
            OnPropertyChanged(nameof(IncomeEntries));
            OnPropertyChanged(nameof(OutcomeCount));
            OnPropertyChanged(nameof(IncomeCount));
        };
    }

    [RelayCommand]
    void ToggleOutcomes() => IsOutcomesExpanded = !IsOutcomesExpanded;

    [RelayCommand]
    void ToggleIncomes() => IsIncomesExpanded = !IsIncomesExpanded;
}
