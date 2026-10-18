using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

public class MainPageObject
{
    private static readonly By AddEntryButton = AppSession.AutomationId("AddEntryButton");
    private static readonly By MonthTitle = AppSession.AutomationId("MonthTitleLabel");
    private static readonly By OutcomeCount = AppSession.AutomationId("OutcomeCountLabel");
    private static readonly By OutcomeTotal = AppSession.AutomationId("OutcomeTotalLabel");
    private static readonly By EntryDescription = AppSession.AutomationId("EntryDescriptionLabel");
    private static readonly By EntryAmount = AppSession.AutomationId("EntryAmountLabel");

    private readonly AppSession _session;

    public MainPageObject(AppSession session)
    {
        _session = session;
    }

    public MainPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(AddEntryButton, TimeSpan.FromSeconds(60));
        _session.WaitForVisible(MonthTitle);
        return this;
    }

    public string MonthTitleText => _session.WaitForVisible(MonthTitle).Text;

    public string OutcomeCountText => _session.WaitForVisible(OutcomeCount).Text;

    public string OutcomeTotalText => _session.WaitForVisible(OutcomeTotal).Text;

    public IReadOnlyList<string> VisibleEntryDescriptions =>
        _session.Driver.FindElements(EntryDescription).Where(e => e.Displayed).Select(e => e.Text).ToList();

    public IReadOnlyList<string> VisibleEntryAmounts =>
        _session.Driver.FindElements(EntryAmount).Where(e => e.Displayed).Select(e => e.Text).ToList();

    public AddEntryPopupObject OpenAddEntry()
    {
        _session.WaitForVisible(AddEntryButton).Click();
        return new AddEntryPopupObject(_session).WaitUntilOpen();
    }

    public MainPageObject WaitForEntry(string description)
    {
        _session.WaitForText(EntryDescription, description);
        return this;
    }
}
