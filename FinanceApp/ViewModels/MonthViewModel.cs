using CommunityToolkit.Mvvm.ComponentModel;
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

    public DateTime Date { get; set; }

    public decimal TotalIncomes => Entries
        .Where(e => e.EntryType == FinancialEntryType.Income)
        .Sum(e => e.EffectiveAmount);

    public decimal TotalOutcomes => Entries
        .Where(e => e.EntryType == FinancialEntryType.Outcome)
        .Sum(e => e.EffectiveAmount);

    public decimal Balance => TotalIncomes - TotalOutcomes;

    public MonthViewModel()
    {
        Entries.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TotalIncomes));
            OnPropertyChanged(nameof(TotalOutcomes));
            OnPropertyChanged(nameof(Balance));
        };
    }
}
