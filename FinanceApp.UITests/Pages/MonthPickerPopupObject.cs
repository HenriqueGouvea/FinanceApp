using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

public class MonthPickerPopupObject
{
    private static readonly By YearLabel = AppSession.AutomationId("PickerYearLabel");
    private static readonly By PreviousYearButton = AppSession.AutomationId("PreviousYearButton");
    private static readonly By NextYearButton = AppSession.AutomationId("NextYearButton");
    private static readonly By MonthOption = AppSession.AutomationId("MonthOption");
    private static readonly By MonthOptionLabel = AppSession.AutomationId("MonthOptionLabel");
    private static readonly By CurrentMonthButton = AppSession.AutomationId("PickerCurrentMonthButton");
    private static readonly By CancelButton = AppSession.AutomationId("CancelMonthPickerButton");

    private readonly AppSession _session;

    public MonthPickerPopupObject(AppSession session)
    {
        _session = session;
    }

    public MonthPickerPopupObject WaitUntilOpen()
    {
        _session.WaitForVisible(YearLabel);
        _session.WaitForVisible(MonthOptionLabel);
        return this;
    }

    public bool IsOpen => _session.IsVisible(YearLabel);

    public int Year => int.Parse(_session.WaitForVisible(YearLabel).Text);

    public IReadOnlyList<string> MonthLabels => _session.VisibleTexts(MonthOptionLabel);

    public bool IsPreviousYearEnabled => _session.WaitForVisible(PreviousYearButton).Enabled;

    public bool IsNextYearEnabled => _session.WaitForVisible(NextYearButton).Enabled;

    public MonthPickerPopupObject ShowYear(int year)
    {
        while (Year != year)
        {
            var expected = (Year < year ? Year + 1 : Year - 1).ToString();
            _session.Tap(Year < year ? NextYearButton : PreviousYearButton);
            _session.WaitForText(YearLabel, expected);
        }

        return this;
    }

    /// <summary>
    /// Enabled state of the month cell in the year currently shown.
    /// </summary>
    public bool IsMonthEnabled(DateTime month) => FindMonthOption(month).Enabled;

    /// <summary>
    /// Switches to the month's year if needed and taps it; the picker closes on selection.
    /// </summary>
    public void SelectMonth(DateTime month)
    {
        ShowYear(month.Year);
        FindMonthOption(month).Click();
        _session.WaitForGone(YearLabel);
    }

    public void SelectCurrentMonth()
    {
        _session.Tap(CurrentMonthButton);
        _session.WaitForGone(YearLabel);
    }

    public void Cancel()
    {
        _session.Tap(CancelButton);
        _session.WaitForGone(YearLabel);
    }

    private IWebElement FindMonthOption(DateTime month)
    {
        var label = AppFormat.MonthAbbreviation(month);
        _session.WaitForVisible(AppSession.AutomationIdWithText("MonthOptionLabel", label));
        return _session.Driver.FindElements(MonthOption)
            .First(o => o.Displayed && o.FindElements(MonthOptionLabel).Any(l => l.Text == label));
    }
}
