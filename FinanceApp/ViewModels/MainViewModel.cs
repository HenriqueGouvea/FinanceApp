using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int MaxMonthsFromToday = 24;

    private static readonly DateTime FirstOfToday =
        new(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Local);

    [ObservableProperty]
    ObservableCollection<MonthViewModel> months = [];

    [ObservableProperty]
    int currentPosition;

    public MonthViewModel? SelectedMonth => Months.ElementAtOrDefault(CurrentPosition);

    private readonly IFinanceService _financeService;

    public MainViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (Months.Count == 0)
        {
            for (int offset = -MaxMonthsFromToday; offset <= MaxMonthsFromToday; offset++)
            {
                Months.Add(CreateMonthViewModel(FirstOfToday.AddMonths(offset)));
            }

            CurrentPosition = MaxMonthsFromToday;
        }

        await LoadNearbyMonthsAsync(CurrentPosition);
    }

    [RelayCommand]
    public async Task RefreshAllAsync()
    {
        foreach (var month in Months)
        {
            month.IsLoaded = false;
        }

        await LoadNearbyMonthsAsync(CurrentPosition);
    }

    [RelayCommand]
    public async Task ClearDatabaseAsync()
    {
        await _financeService.ClearAllDataAsync();

        foreach (var month in Months)
        {
            month.Entries.Clear();
            month.IsLoaded = false;
        }

        await LoadNearbyMonthsAsync(CurrentPosition);
    }

    partial void OnCurrentPositionChanged(int value)
    {
        _ = LoadNearbyMonthsAsync(value);
    }

    private async Task LoadNearbyMonthsAsync(int position)
    {
        var from = Math.Max(0, position - 2);
        var to = Math.Min(Months.Count - 1, position + 2);

        for (int i = from; i <= to; i++)
        {
            var month = Months[i];
            if (month.IsLoaded)
                continue;

            var entries = await _financeService.GetMergedEntriesForMonthAsync(month.Date.Year, month.Date.Month);

            month.Entries.Clear();
            foreach (var entry in entries)
                month.Entries.Add(entry);

            month.IsLoaded = true;
        }
    }

    private static MonthViewModel CreateMonthViewModel(DateTime date) => new()
    {
        MonthName = date.ToString("MMMM yyyy"),
        Date = new DateTime(date.Year, date.Month, 1)
    };
}
