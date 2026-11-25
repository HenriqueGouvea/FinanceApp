using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// "Relatórios": the month's outcomes or incomes by category, as a table, a pie chart or a column chart.
/// </summary>
public class ReportsPageObject
{
    private static readonly By MonthTitle = AppSession.AutomationId("ReportMonthTitleLabel");
    private static readonly By NextMonthButton = AppSession.AutomationId("ReportNextMonthButton");
    private static readonly By OutcomesOption = AppSession.AutomationId("ReportOutcomesOption");
    private static readonly By IncomesOption = AppSession.AutomationId("ReportIncomesOption");
    private static readonly By ColumnsOption = AppSession.AutomationId("ReportColumnsOption");
    private static readonly By Total = AppSession.AutomationId("ReportTotalLabel");
    private static readonly By Row = AppSession.AutomationId("ReportRow");
    private static readonly By RowName = AppSession.AutomationId("ReportRowNameLabel");
    private static readonly By RowAmount = AppSession.AutomationId("ReportRowAmountLabel");
    private static readonly By RowShare = AppSession.AutomationId("ReportRowShareLabel");
    private static readonly By Chart = AppSession.AutomationId("ReportChart");
    private static readonly By LegendName = AppSession.AutomationId("ReportLegendNameLabel");
    private static readonly By Empty = AppSession.AutomationId("ReportEmptyLabel");
    private static readonly By CategoryEntry = AppSession.AutomationId("ReportCategoryEntry");
    private static readonly By CategoryEntryDescription = AppSession.AutomationId("ReportCategoryEntryDescriptionLabel");
    private static readonly By CategoryEntryAmount = AppSession.AutomationId("ReportCategoryEntryAmountLabel");
    private static readonly By CategoryEntryStatus = AppSession.AutomationId("ReportCategoryEntryStatusLabel");
    private const string SharedEntriesNoteId = "ReportSharedEntriesNote";
    private const string CategoryEntriesTitleId = "ReportCategoryEntriesTitle";

    private readonly AppSession _session;

    public ReportsPageObject(AppSession session)
    {
        _session = session;
    }

    public ReportsPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(MonthTitle);
        _session.WaitForVisible(Total);
        return this;
    }

    /// <summary>
    /// The month shown, without the picker's "▾" (e.g. "Novembro 2026", see <see cref="AppFormat.MonthTitle"/>).
    /// </summary>
    public string Month => _session.WaitForVisible(MonthTitle).Text.TrimEnd('▾', ' ');

    public string TotalText => _session.WaitForVisible(Total).Text;

    public bool IsEmpty => _session.IsVisible(Empty);

    public bool IsChartVisible => _session.IsVisible(Chart);

    /// <summary>
    /// The table's rows, top to bottom: the category, its total and its share (e.g. ("Saúde", "R$ 80,00", "7%")).
    /// </summary>
    public IReadOnlyList<(string Name, string Amount, string Share)> Rows =>
        [
            .. _session.Driver.FindElements(Row).Where(r => r.Displayed).Select(r => (
                r.FindElement(RowName).Text,
                r.FindElement(RowAmount).Text,
                r.FindElement(RowShare).Text))
        ];

    /// <summary>
    /// The categories under the chart, e.g. "🩺 Saúde".
    /// </summary>
    public IReadOnlyList<string> LegendNames => _session.VisibleTexts(LegendName);

    public bool IsSharedEntriesNoteVisible => _session.TryScrollTo(SharedEntriesNoteId) is not null;

    /// <summary>
    /// Waits for the table to list that category first, e.g. after switching to incomes or to another month.
    /// </summary>
    public ReportsPageObject WaitForFirstRow(string name)
    {
        _session.WaitFor(() => Rows.FirstOrDefault().Name == name, $"the report to list \"{name}\" first");
        return this;
    }

    public ReportsPageObject ShowIncomes()
    {
        _session.Tap(IncomesOption);
        return this;
    }

    public ReportsPageObject ShowOutcomes()
    {
        _session.Tap(OutcomesOption);
        return this;
    }

    public ReportsPageObject ShowTable()
    {
        _session.ScrollToAndTap("ReportTableOption");
        _session.WaitForVisible(Row);
        return this;
    }

    public ReportsPageObject ShowPie()
    {
        _session.ScrollToAndTap("ReportPieOption");
        _session.WaitForVisible(Chart);
        return this;
    }

    public ReportsPageObject ShowColumns()
    {
        _session.Tap(ColumnsOption);
        _session.WaitForVisible(Chart);
        return this;
    }

    /// <summary>
    /// Taps the table's row of that category, which lists its entries under the report.
    /// </summary>
    public ReportsPageObject TapRow(string name)
    {
        var row = _session.Driver.FindElements(Row).First(r => r.Displayed && r.FindElement(RowName).Text == name);
        _session.Tap(row);
        return WaitForCategoryEntries(name);
    }

    /// <summary>
    /// Taps the chart at a point given as fractions of its width and height (e.g. a slice of the pie), which lists
    /// that category's entries under the report.
    /// </summary>
    public ReportsPageObject TapChartAt(double xFraction, double yFraction, string expectedCategory)
    {
        _session.TapInside(_session.WaitForVisible(Chart), xFraction, yFraction);
        return WaitForCategoryEntries(expectedCategory);
    }

    /// <summary>
    /// The category whose entries are listed under the report.
    /// </summary>
    public string CategoryEntriesTitle => _session.ScrollTo(CategoryEntriesTitleId).Text;

    /// <summary>
    /// The entries listed for the tapped category, top to bottom: (description, amount, status), e.g.
    /// ("Escola", "R$ 1.000,00", "Pendente").
    /// </summary>
    public IReadOnlyList<(string Description, string Amount, string Status)> CategoryEntries
    {
        get
        {
            _session.TryScrollTo(CategoryEntriesTitleId);
            return
            [
                .. _session.ItemsWhileScrollingDown(CategoryEntry, e =>
                {
                    var labels = new[] { CategoryEntryDescription, CategoryEntryAmount, CategoryEntryStatus }
                        .Select(l => e.FindElements(l).FirstOrDefault(x => x.Displayed))
                        .ToList();
                    return labels.Any(l => l is null) ? null : Tuple.Create(labels[0]!.Text, labels[1]!.Text, labels[2]!.Text);
                })
                .Select(t => (t.Item1, t.Item2, t.Item3))
            ];
        }
    }

    private ReportsPageObject WaitForCategoryEntries(string name)
    {
        _session.WaitFor(() => _session.TryScrollTo(CategoryEntriesTitleId)?.Text == name, $"the entries of \"{name}\" to show");
        return this;
    }

    public ReportsPageObject GoToNextMonth()
    {
        var month = Month;
        _session.Tap(NextMonthButton);
        _session.WaitFor(() => Month != month, "the report to show the next month");
        return this;
    }
}
