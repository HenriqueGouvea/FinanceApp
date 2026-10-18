using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

public class MainPageObject
{
    private static readonly By AddEntryButton = AppSession.AutomationId("AddEntryButton");
    private static readonly By MonthTitle = AppSession.AutomationId("MonthTitleLabel");
    private static readonly By SummaryIncome = AppSession.AutomationId("SummaryIncomeLabel");
    private static readonly By SummaryOutcome = AppSession.AutomationId("SummaryOutcomeLabel");
    private static readonly By SummaryBalance = AppSession.AutomationId("SummaryBalanceLabel");
    private static readonly By OutcomesHeader = AppSession.AutomationId("OutcomesHeader");
    private static readonly By OutcomeCount = AppSession.AutomationId("OutcomeCountLabel");
    private static readonly By OutcomeTotal = AppSession.AutomationId("OutcomeTotalLabel");
    private static readonly By IncomesHeader = AppSession.AutomationId("IncomesHeader");
    private static readonly By IncomeCount = AppSession.AutomationId("IncomeCountLabel");
    private static readonly By IncomeTotal = AppSession.AutomationId("IncomeTotalLabel");
    private static readonly By EntryRow = AppSession.AutomationId("EntryRow");
    private static readonly By EntryDescription = AppSession.AutomationId("EntryDescriptionLabel");
    private static readonly By EntryAmount = AppSession.AutomationId("EntryAmountLabel");
    private static readonly By EntryCategory = AppSession.AutomationId("EntryCategoryLabel");
    private static readonly By EntryInstallment = AppSession.AutomationId("EntryInstallmentLabel");
    private static readonly By DeleteAction = AppSession.AutomationId("DeleteEntryAction");
    private static readonly By FlyoutButton =
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.ImageButton\").instance(0)");

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
    public string SummaryIncomeText => _session.WaitForVisible(SummaryIncome).Text;
    public string SummaryOutcomeText => _session.WaitForVisible(SummaryOutcome).Text;
    public string SummaryBalanceText => _session.WaitForVisible(SummaryBalance).Text;
    public string OutcomeCountText => _session.WaitForVisible(OutcomeCount).Text;
    public string OutcomeTotalText => _session.WaitForVisible(OutcomeTotal).Text;
    public string IncomeCountText => _session.WaitForVisible(IncomeCount).Text;
    public string IncomeTotalText => _session.WaitForVisible(IncomeTotal).Text;

    public IReadOnlyList<string> VisibleEntryDescriptions => _session.VisibleTexts(EntryDescription);
    public IReadOnlyList<string> VisibleEntryAmounts => _session.VisibleTexts(EntryAmount);
    public IReadOnlyList<string> VisibleEntryCategories => _session.VisibleTexts(EntryCategory);
    public IReadOnlyList<string> VisibleInstallmentLabels => _session.VisibleTexts(EntryInstallment);

    public bool IsEntryVisible(string description) => _session.IsVisible(EntryByDescription(description));

    public AddEntryPopupObject OpenAddEntry()
    {
        _session.Tap(AddEntryButton);
        return new AddEntryPopupObject(_session).WaitUntilOpen();
    }

    public MainPageObject AddEntry(
        string description,
        string recurrence,
        string amount,
        bool isIncome = false,
        string? installments = null,
        string? category = null)
    {
        var popup = OpenAddEntry().EnterDescription(description);

        if (isIncome)
            popup.SelectIncome();

        popup.SelectRecurrence(recurrence);

        if (installments is not null)
            popup.EnterInstallments(installments);

        popup.EnterAmount(amount);

        if (category is not null)
            popup.SelectCategory(category);

        popup.Save();
        return WaitForEntry(description);
    }

    public AddEntryPopupObject OpenEntry(string description)
    {
        _session.Tap(EntryByDescription(description));
        return new AddEntryPopupObject(_session).WaitUntilOpen();
    }

    public void TapEntry(string description) => _session.Tap(EntryByDescription(description));

    public MainPageObject WaitForEntry(string description)
    {
        _session.WaitForVisible(EntryByDescription(description));
        return this;
    }

    public MainPageObject WaitForEntryGone(string description)
    {
        _session.WaitForGone(EntryByDescription(description));
        return this;
    }

    public MainPageObject RevealActions(string description)
    {
        _session.LongPress(_session.WaitForVisible(EntryByDescription(description)));
        _session.WaitFor(() => IsDeleteActionRevealed, "the entry actions menu to slide in");
        return this;
    }

    /// <summary>
    /// The actions panel is always in the view tree; it is "revealed" when slid inside its row instead of past the right edge.
    /// </summary>
    public bool IsDeleteActionRevealed => FindRevealedDeleteAction() is not null;

    public MainPageObject WaitForActionsHidden()
    {
        _session.WaitFor(() => !IsDeleteActionRevealed, "the entry actions menu to slide out");
        return this;
    }

    public void TapDeleteAction() =>
        (FindRevealedDeleteAction() ?? throw new InvalidOperationException("No entry actions menu is revealed.")).Click();

    /// <summary>
    /// Long-presses the entry, taps "Excluir" and picks the option in the native action sheet.
    /// </summary>
    public MainPageObject DeleteEntry(string description, string option)
    {
        RevealActions(description);
        TapDeleteAction();
        ChooseDialogOption(option);
        return this;
    }

    public void ChooseDialogOption(string option) => _session.Tap(AppSession.DialogOption(option));

    public void TapMonthTitle() => _session.Tap(MonthTitle);

    public MainPageObject GoToNextMonth()
    {
        var expected = AppFormat.MonthTitle(CurrentMonth.AddMonths(1));
        _session.SwipeHorizontally("left");
        _session.WaitForText(MonthTitle, expected);
        return this;
    }

    public MainPageObject GoToPreviousMonth()
    {
        var expected = AppFormat.MonthTitle(CurrentMonth.AddMonths(-1));
        _session.SwipeHorizontally("right");
        _session.WaitForText(MonthTitle, expected);
        return this;
    }

    public DateTime CurrentMonth
    {
        get
        {
            var title = MonthTitleText;
            return Enumerable.Range(-24, 49)
                .Select(offset => AppFormat.CurrentMonth.AddMonths(offset))
                .First(month => AppFormat.MonthTitle(month) == title);
        }
    }

    public MainPageObject ToggleOutcomes()
    {
        _session.Tap(OutcomesHeader);
        return this;
    }

    public MainPageObject ToggleIncomes()
    {
        _session.Tap(IncomesHeader);
        return this;
    }

    public CategoriesPageObject OpenCategories()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.CategoriesMenuItem));
        return new CategoriesPageObject(_session).WaitUntilLoaded();
    }

    private IWebElement? FindRevealedDeleteAction()
    {
        var rows = _session.Driver.FindElements(EntryRow).Where(r => r.Displayed).ToList();
        if (rows.Count == 0)
            return null;

        var rowRight = rows.Max(r => r.Location.X + r.Size.Width);
        return _session.Driver.FindElements(DeleteAction)
            .FirstOrDefault(a => a.Displayed && a.Location.X + a.Size.Width / 2 < rowRight);
    }

    private static By EntryByDescription(string description) =>
        AppSession.AutomationIdWithText("EntryDescriptionLabel", description);
}
