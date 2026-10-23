using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FinanceApp.ViewModels;

public partial class MonthPickerViewModel : ObservableObject
{
    private Action<DateTime>? _onSelected;
    private DateTime _selectedMonth;
    private DateTime _currentMonth;
    private DateTime _firstMonth;
    private DateTime _lastMonth;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextYearCommand))]
    int year;

    public ObservableCollection<MonthOption> MonthOptions { get; } = [];

    public void Prepare(DateTime selectedMonth, DateTime currentMonth, DateTime firstMonth, DateTime lastMonth)
    {
        _selectedMonth = FirstOfMonth(selectedMonth);
        _currentMonth = FirstOfMonth(currentMonth);
        _firstMonth = FirstOfMonth(firstMonth);
        _lastMonth = FirstOfMonth(lastMonth);

        if (Year == _selectedMonth.Year)
            BuildMonthOptions();
        else
            Year = _selectedMonth.Year;
    }

    public void SetSelectCallback(Action<DateTime> onSelected)
    {
        _onSelected = onSelected;
    }

    partial void OnYearChanged(int value) => BuildMonthOptions();

    private bool CanGoToPreviousYear() => Year > _firstMonth.Year;

    private bool CanGoToNextYear() => Year < _lastMonth.Year;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousYear))]
    void PreviousYear() => Year--;

    [RelayCommand(CanExecute = nameof(CanGoToNextYear))]
    void NextYear() => Year++;

    [RelayCommand]
    void SelectMonth(MonthOption option)
    {
        if (option.IsEnabled)
            _onSelected?.Invoke(option.Date);
    }

    [RelayCommand]
    void SelectCurrentMonth() => _onSelected?.Invoke(_currentMonth);

    private void BuildMonthOptions()
    {
        MonthOptions.Clear();

        for (int month = 1; month <= 12; month++)
        {
            var date = new DateTime(Year, month, 1);
            MonthOptions.Add(new MonthOption(
                Date: date,
                Label: FormatMonthLabel(month),
                IsSelected: date == _selectedMonth,
                IsCurrent: date == _currentMonth,
                IsEnabled: date >= _firstMonth && date <= _lastMonth));
        }
    }

    // pt-BR abbreviations are lowercase with a trailing dot (e.g. "out.").
    private static string FormatMonthLabel(int month)
    {
        var name = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month).TrimEnd('.');
        return CultureInfo.CurrentCulture.TextInfo.ToUpper(name[0]) + name[1..];
    }

    private static DateTime FirstOfMonth(DateTime date) => new(date.Year, date.Month, 1);
}
