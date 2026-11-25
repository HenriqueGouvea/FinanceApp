using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// "Relatórios": a month's outcomes or incomes by category, as a table, a pie chart or a column chart.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    public const string UncategorizedName = "Sem categoria";
    public const string UncategorizedIcon = "🏷️";
    public const string UncategorizedColor = "#B8A9C9";

    private readonly IFinanceService _financeService;
    private readonly DateTime _currentMonth;
    private CategoryReport? _report;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    [NotifyCanExecuteChangedFor(nameof(PreviousMonthCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextMonthCommand))]
    DateTime month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOutcome))]
    [NotifyPropertyChangedFor(nameof(TotalLabel))]
    [NotifyPropertyChangedFor(nameof(EmptyLabel))]
    FinancialEntryType entryType = FinancialEntryType.Outcome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTable))]
    [NotifyPropertyChangedFor(nameof(IsPie))]
    [NotifyPropertyChangedFor(nameof(IsColumns))]
    [NotifyPropertyChangedFor(nameof(IsChart))]
    ReportView view = ReportView.Table;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(IsChart))]
    [NotifyPropertyChangedFor(nameof(ShowsSelectHint))]
    IReadOnlyList<CategoryReportRow> rows = [];

    [ObservableProperty]
    decimal monthTotal;

    [ObservableProperty]
    bool categoriesExceedTotal;

    /// <summary>
    /// The category tapped in the table, the chart or its legend, whose entries are listed under the report; null when none.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRow))]
    [NotifyPropertyChangedFor(nameof(ShowsSelectHint))]
    CategoryReportRow? selectedRow;

    public ReportsViewModel(IFinanceService financeService, TimeProvider timeProvider)
    {
        _financeService = financeService;
        var now = timeProvider.GetLocalNow();
        _currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);
        Month = _currentMonth;
    }

    public DateTime CurrentMonth => _currentMonth;
    public DateTime FirstMonth => _currentMonth.AddMonths(-MainViewModel.MaxMonthsFromToday);
    public DateTime LastMonth => _currentMonth.AddMonths(MainViewModel.MaxMonthsFromToday);

    public string MonthTitle => MainViewModel.FormatMonthName(Month);
    public bool IsOutcome => EntryType == FinancialEntryType.Outcome;
    public bool IsTable => View == ReportView.Table;
    public bool IsPie => View == ReportView.Pie;
    public bool IsColumns => View == ReportView.Columns;
    public bool HasRows => Rows.Count > 0;
    public bool IsEmpty => Rows.Count == 0;
    /// <summary>
    /// The charts' legend (a short list of the categories) shows under a chart, but not under the table.
    /// </summary>
    public bool IsChart => !IsTable && HasRows;
    public string TotalLabel => IsOutcome ? "Despesas do mês" : "Receitas do mês";
    public string EmptyLabel => IsOutcome ? "Nenhuma despesa neste mês." : "Nenhuma receita neste mês.";

    public bool HasSelectedRow => SelectedRow is not null;
    public bool ShowsSelectHint => HasRows && !HasSelectedRow;

    partial void OnEntryTypeChanged(FinancialEntryType value)
    {
        SelectedRow = null;
        BuildRows();
    }

    [RelayCommand]
    async Task Load() => await LoadMonthAsync(Month);

    public async Task GoToMonthAsync(DateTime date)
    {
        var target = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Local);
        if (target < FirstMonth || target > LastMonth)
            return;

        await LoadMonthAsync(target);
    }

    private bool CanGoToPreviousMonth() => Month > FirstMonth;

    private bool CanGoToNextMonth() => Month < LastMonth;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousMonth))]
    Task PreviousMonth() => GoToMonthAsync(Month.AddMonths(-1));

    [RelayCommand(CanExecute = nameof(CanGoToNextMonth))]
    Task NextMonth() => GoToMonthAsync(Month.AddMonths(1));

    [RelayCommand]
    void SelectOutcomes() => EntryType = FinancialEntryType.Outcome;

    [RelayCommand]
    void SelectIncomes() => EntryType = FinancialEntryType.Income;

    [RelayCommand]
    void SelectView(ReportView value) => View = value;

    /// <summary>
    /// Lists the tapped category's entries; tapping it again hides them.
    /// </summary>
    [RelayCommand]
    void SelectRow(CategoryReportRow? row) => SelectedRow = row is null || ReferenceEquals(row, SelectedRow) ? null : row;

    [RelayCommand]
    void CloseSelectedRow() => SelectedRow = null;

    private async Task LoadMonthAsync(DateTime target)
    {
        Month = target;
        var report = await _financeService.GetCategoryReportAsync(target.Year, target.Month);
        // A slower load of a month left behind must not overwrite the one now on screen.
        if (Month != target)
            return;

        _report = report;
        BuildRows();
    }

    private void BuildRows()
    {
        if (_report is null)
            return;

        var totals = _report.TotalsOf(EntryType).ToList();
        var sum = totals.Sum(t => t.Amount);

        Rows =
        [
            .. totals.Select(t => new CategoryReportRow(
                t.Category?.Name ?? UncategorizedName,
                t.Category?.Icon ?? UncategorizedIcon,
                t.Category?.Color ?? UncategorizedColor,
                t.Amount,
                sum > 0 ? (double)(t.Amount / sum) : 0,
                t.EntryCount)
            {
                CategoryId = t.Category?.Id,
                Entries = [.. t.Entries.Select(CategoryReportEntry.From)]
            })
        ];
        // After a reload (another month, or back from "Início"), the same category stays open if it is still there.
        var selectedId = SelectedRow?.CategoryId;
        SelectedRow = SelectedRow is null ? null : Rows.FirstOrDefault(r => r.CategoryId == selectedId);
        MonthTotal = _report.MonthTotalOf(EntryType);
        CategoriesExceedTotal = sum > MonthTotal;
    }
}
