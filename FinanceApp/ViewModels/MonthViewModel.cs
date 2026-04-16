using CommunityToolkit.Mvvm.ComponentModel;
using FinanceApp.Models;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    public ObservableCollection<FinancialEntry> Entries { get; } = [];

    [ObservableProperty]
    private string monthName = string.Empty;

    public DateTime Date { get; set; }

    public decimal TotalIncomes => 0m;
    public decimal TotalOutcomes => Entries.Sum(e => e.Amount);
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
