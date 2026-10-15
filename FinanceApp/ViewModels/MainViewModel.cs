using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace FinanceApp.ViewModels;

public partial class MainViewModel : ObservableObject, IRecipient<CategoriesChangedMessage>
{
    private const int MaxMonthsFromToday = 24;
    private const string IncomesExpandedKey = "incomes_expanded";
    private const string OutcomesExpandedKey = "outcomes_expanded";

    private static readonly DateTime FirstOfToday =
        new(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Local);

    [ObservableProperty]
    ObservableCollection<MonthViewModel> months = [];

    [ObservableProperty]
    int currentPosition;

    public MonthViewModel? SelectedMonth => Months.ElementAtOrDefault(CurrentPosition);

    private readonly IFinanceService _financeService;
    private readonly IPreferencesService _preferencesService;
    private bool _isSyncing;

    public MainViewModel(IFinanceService financeService, IPreferencesService preferencesService, IMessenger messenger)
    {
        _financeService = financeService;
        _preferencesService = preferencesService;
        messenger.Register(this);
    }

    public async void Receive(CategoriesChangedMessage message) => await RefreshAllAsync();

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

    public async Task DeleteEntryAsync(MonthlyEntryProjection projection, EntryDeleteScope scope)
    {
        if (SelectedMonth is not { } month)
            return;

        await _financeService.DeleteEntryAsync(projection.FinancialEntryId, month.Date.Year, month.Date.Month, scope);
        await RefreshAllAsync();
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
        foreach (var month in Months)
            month.HideEntryActions();

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

    private MonthViewModel CreateMonthViewModel(DateTime date)
    {
        var vm = new MonthViewModel
        {
            MonthName = FormatMonthName(date),
            Date = new DateTime(date.Year, date.Month, 1),
            IsIncomesExpanded = _preferencesService.Get(IncomesExpandedKey, true),
            IsOutcomesExpanded = _preferencesService.Get(OutcomesExpandedKey, true)
        };

        vm.PropertyChanged += OnMonthViewModelPropertyChanged;
        return vm;
    }

    // Some cultures (e.g. pt-BR) write month names in lowercase.
    private static string FormatMonthName(DateTime date)
    {
        var name = date.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        return CultureInfo.CurrentCulture.TextInfo.ToUpper(name[0]) + name[1..];
    }

    private void OnMonthViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncing || sender is not MonthViewModel changedVm) return;

        _isSyncing = true;
        try
        {
            if (e.PropertyName == nameof(MonthViewModel.IsIncomesExpanded))
            {
                _preferencesService.Set(IncomesExpandedKey, changedVm.IsIncomesExpanded);
                foreach (var m in Months.Where(m => m != changedVm))
                    m.IsIncomesExpanded = changedVm.IsIncomesExpanded;
            }
            else if (e.PropertyName == nameof(MonthViewModel.IsOutcomesExpanded))
            {
                _preferencesService.Set(OutcomesExpandedKey, changedVm.IsOutcomesExpanded);
                foreach (var m in Months.Where(m => m != changedVm))
                    m.IsOutcomesExpanded = changedVm.IsOutcomesExpanded;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }
}
