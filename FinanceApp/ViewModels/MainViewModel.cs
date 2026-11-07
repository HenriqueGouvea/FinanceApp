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

public partial class MainViewModel : ObservableObject,
    IRecipient<CategoriesChangedMessage>,
    IRecipient<PaymentSourcesChangedMessage>,
    IRecipient<SetupCompletedMessage>
{
    private const int MaxMonthsFromToday = 24;
    private const string IncomesExpandedKey = "incomes_expanded";
    private const string OutcomesExpandedKey = "outcomes_expanded";
    private const string ShowSwipeHintKey = "show_month_swipe_hint";
    private const double CompactHeaderScrollY = 64;

    private static readonly DateTime FirstOfToday =
        new(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Local);

    [ObservableProperty]
    ObservableCollection<MonthViewModel> months = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonth))]
    [NotifyPropertyChangedFor(nameof(IsAwayFromCurrentMonth))]
    [NotifyPropertyChangedFor(nameof(GoToCurrentMonthLabel))]
    [NotifyPropertyChangedFor(nameof(IsSelectedMonthLoading))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousMonthCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextMonthCommand))]
    int currentPosition;

    [ObservableProperty]
    bool showSwipeHint;

    /// <summary>
    /// The month header has scrolled out of view, so the top bar shows the month pill instead of the app name.
    /// </summary>
    [ObservableProperty]
    bool isHeaderCompact;

    public MonthViewModel? SelectedMonth => Months.ElementAtOrDefault(CurrentPosition);

    /// <summary>
    /// The month on screen is still fetching its entries, so the page shows that something is happening instead of an
    /// empty (or outdated) month.
    /// </summary>
    public bool IsSelectedMonthLoading => SelectedMonth is { IsLoaded: false };

    public DateTime CurrentMonthDate => FirstOfToday;
    public DateTime FirstMonthDate => FirstOfToday.AddMonths(-MaxMonthsFromToday);
    public DateTime LastMonthDate => FirstOfToday.AddMonths(MaxMonthsFromToday);

    public bool IsAwayFromCurrentMonth => Months.Count > 0 && CurrentPosition != MaxMonthsFromToday;

    // The arrow points towards the current month.
    public string GoToCurrentMonthLabel => CurrentPosition > MaxMonthsFromToday ? "‹ Mês atual" : "Mês atual ›";

    private readonly IFinanceService _financeService;
    private readonly IPreferencesService _preferencesService;
    private bool _isSyncing;
    private bool _isInitialized;

    public MainViewModel(IFinanceService financeService, IPreferencesService preferencesService, IMessenger messenger)
    {
        _financeService = financeService;
        _preferencesService = preferencesService;
        ShowSwipeHint = preferencesService.Get(ShowSwipeHintKey, true);
        messenger.RegisterAll(this);
    }

    public async void Receive(CategoriesChangedMessage message) => await RefreshAllAsync();
    public async void Receive(PaymentSourcesChangedMessage message) => await RefreshAllAsync();
    public async void Receive(SetupCompletedMessage message) => await RefreshAllAsync();

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
            _isInitialized = true;
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

    public async Task ToggleEntryStatusAsync(MonthlyEntryProjection projection)
    {
        if (SelectedMonth is not { } month)
            return;

        month.HideEntryActions();

        var status = projection.IsPaid ? FinancialMonthlyEntryStatus.Pending : FinancialMonthlyEntryStatus.Paid;
        await _financeService.SetEntryStatusAsync(projection.FinancialEntryId, month.Date.Year, month.Date.Month, status);
        await RefreshAllAsync();
    }

    /// <summary>
    /// Pays every entry of a credit card's bill, or turns a paid bill back to pending.
    /// </summary>
    public async Task ToggleCreditCardBillStatusAsync(CreditCardBill bill)
    {
        if (SelectedMonth is not { } month)
            return;

        month.HideEntryActions();

        var status = bill.IsPaid ? FinancialMonthlyEntryStatus.Pending : FinancialMonthlyEntryStatus.Paid;
        await _financeService.SetCreditCardBillStatusAsync(bill.Card.Id, month.Date.Year, month.Date.Month, status);
        await RefreshAllAsync();
    }

    public void GoToMonth(DateTime date)
    {
        var index = (date.Year - FirstMonthDate.Year) * 12 + date.Month - FirstMonthDate.Month;
        if (index >= 0 && index < Months.Count)
            CurrentPosition = index;
    }

    /// <summary>
    /// Called as the selected month scrolls: past its month header the top bar takes over showing the month.
    /// </summary>
    public void UpdateHeaderCompact(double scrollY) => IsHeaderCompact = scrollY > CompactHeaderScrollY;

    [RelayCommand]
    void GoToCurrentMonth() => GoToMonth(FirstOfToday);

    [RelayCommand(CanExecute = nameof(CanGoToPreviousMonth))]
    void GoToPreviousMonth() => CurrentPosition--;

    [RelayCommand(CanExecute = nameof(CanGoToNextMonth))]
    void GoToNextMonth() => CurrentPosition++;

    private bool CanGoToPreviousMonth() => CurrentPosition > 0;

    private bool CanGoToNextMonth() => CurrentPosition < Months.Count - 1;

    partial void OnCurrentPositionChanged(int value)
    {
        IsHeaderCompact = false;

        foreach (var month in Months)
            month.HideEntryActions();

        // Once the user has changed month they know navigation exists, so the hint is never shown again.
        if (_isInitialized && ShowSwipeHint)
        {
            ShowSwipeHint = false;
            _preferencesService.Set(ShowSwipeHintKey, false);
        }

        _ = LoadNearbyMonthsAsync(value);
    }

    private async Task LoadNearbyMonthsAsync(int position)
    {
        // The month on screen first, then its neighbours, closest first, so the user waits as little as possible.
        int[] offsets = [0, 1, -1, 2, -2];
        foreach (var i in offsets.Select(o => position + o).Where(i => i >= 0 && i < Months.Count))
        {
            var month = Months[i];
            if (month.IsLoaded)
                continue;

            var entries = await _financeService.GetMergedEntriesForMonthAsync(month.Date.Year, month.Date.Month);

            month.ReplaceEntries(entries);
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
        if (e.PropertyName == nameof(MonthViewModel.IsLoaded) && sender == SelectedMonth)
            OnPropertyChanged(nameof(IsSelectedMonthLoading));

        SyncMonthPreferences(sender, e);
    }

    private void SyncMonthPreferences(object? sender, PropertyChangedEventArgs e)
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
            else if (e.PropertyName == nameof(MonthViewModel.SummaryPage))
            {
                foreach (var m in Months.Where(m => m != changedVm))
                    m.SummaryPage = changedVm.SummaryPage;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }
}
