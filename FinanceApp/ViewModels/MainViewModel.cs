using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<MonthViewModel> Months { get; set; } = [];

    [ObservableProperty]
    private int currentPosition;

    public MonthViewModel? SelectedMonth => Months.ElementAtOrDefault(CurrentPosition);

    private readonly IFinanceService _financeService;

    public MainViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var today = DateTime.Today;
        var monthsToLoad = new[] { today.AddMonths(-1), today, today.AddMonths(1) };

        if (Months.Count == 0)
        {
            foreach (var date in monthsToLoad)
            {
                Months.Add(new MonthViewModel
                {
                    MonthName = date.ToString("MMMM yyyy"),
                    Date = new DateTime(date.Year, date.Month, 1)
                });
            }

            CurrentPosition = 1;
        }

        for (int i = 0; i < Months.Count; i++)
        {
            var entries = await _financeService.GetEntriesByMonthAsync(Months[i].Date);

            Months[i].Entries.Clear();

            foreach (var entry in entries)
                Months[i].Entries.Add(entry);
        }
    }
}
