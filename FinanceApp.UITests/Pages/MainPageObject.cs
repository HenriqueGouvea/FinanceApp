using System.Drawing;
using System.Runtime.CompilerServices;
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
    private static readonly By SummaryReceived = AppSession.AutomationId("SummaryReceivedLabel");
    private static readonly By SummaryToReceive = AppSession.AutomationId("SummaryToReceiveLabel");
    private static readonly By SummaryPaid = AppSession.AutomationId("SummaryPaidLabel");
    private static readonly By SummaryToPay = AppSession.AutomationId("SummaryToPayLabel");
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
    private static readonly By MarkPaidAction = AppSession.AutomationId("MarkPaidEntryAction");
    private static readonly By MarkPendingAction = AppSession.AutomationId("MarkPendingEntryAction");
    private static readonly By EntryPaidIcon = AppSession.AutomationId("EntryPaidIcon");
    private static readonly By EntryStatus = AppSession.AutomationId("EntryStatusLabel");
    private static readonly By EntryPartialIcon = AppSession.AutomationId("EntryPartialIcon");
    private static readonly By EntryPartial = AppSession.AutomationId("EntryPartialLabel");
    private static readonly By EntryEstimatedIcon = AppSession.AutomationId("EntryEstimatedIcon");
    private static readonly By MonthTitleButton = AppSession.AutomationId("MonthTitleButton");
    private static readonly By SummaryBar = AppSession.AutomationId("SummaryBar");
    private static readonly By GoToCurrentMonthButton = AppSession.AutomationId("GoToCurrentMonthButton");
    private static readonly By PreviousMonthButton = AppSession.AutomationId("PreviousMonthButton");
    private static readonly By NextMonthButton = AppSession.AutomationId("NextMonthButton");
    private static readonly By SwipeHint = AppSession.AutomationId("SwipeHintLabel");
    private static readonly By MonthPickerBadge = AppSession.AutomationId("MonthPickerBadge");
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
    public string SummaryReceivedText => _session.WaitForVisible(SummaryReceived).Text;
    public string SummaryToReceiveText => _session.WaitForVisible(SummaryToReceive).Text;
    public string SummaryPaidText => _session.WaitForVisible(SummaryPaid).Text;
    public string SummaryToPayText => _session.WaitForVisible(SummaryToPay).Text;
    public string OutcomeCountText => _session.WaitForVisible(OutcomeCount).Text;
    public string OutcomeTotalText => _session.WaitForVisible(OutcomeTotal).Text;
    public string IncomeCountText => _session.WaitForVisible(IncomeCount).Text;
    public string IncomeTotalText => _session.WaitForVisible(IncomeTotal).Text;

    public IReadOnlyList<string> VisibleEntryDescriptions => _session.VisibleTexts(EntryDescription);
    public IReadOnlyList<string> VisibleEntryAmounts => _session.VisibleTexts(EntryAmount);
    public IReadOnlyList<string> VisibleEntryCategories => _session.VisibleTexts(EntryCategory);
    public IReadOnlyList<string> VisibleInstallmentLabels => _session.VisibleTexts(EntryInstallment);

    public bool IsEntryVisible(string description) => _session.IsVisible(EntryByDescription(description));

    /// <summary>
    /// Waits for a state that the page reaches asynchronously, e.g. after a save refreshes the month.
    /// </summary>
    public MainPageObject WaitFor(Func<bool> condition, [CallerArgumentExpression(nameof(condition))] string description = "")
    {
        _session.WaitFor(condition, description);
        return this;
    }

    public AddEntryPopupObject OpenAddEntry()
    {
        _session.Tap(AddEntryButton);
        return new AddEntryPopupObject(_session).WaitUntilOpen();
    }

    public MainPageObject AddEntry(
        string description,
        string recurrence,
        decimal amount,
        bool isIncome = false,
        string? installments = null,
        string? category = null,
        bool isEstimated = false)
    {
        var popup = OpenAddEntry().EnterDescription(description);

        if (isIncome)
            popup.SelectIncome();

        popup.SelectRecurrence(recurrence);

        if (installments is not null)
            popup.EnterInstallments(installments);

        popup.EnterAmount(amount);

        if (isEstimated)
            popup.SetEstimated(true);

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
    public bool IsDeleteActionRevealed => FindRevealedAction(DeleteAction) is not null;

    public MainPageObject WaitForActionsHidden()
    {
        _session.WaitFor(() => !IsDeleteActionRevealed, "the entry actions menu to slide out");
        return this;
    }

    public void TapDeleteAction() => TapRevealedAction(DeleteAction);

    /// <summary>
    /// Text of the status action in the revealed menu ("Pagar", "Receber" or "Pendente").
    /// </summary>
    public string RevealedStatusActionText =>
        (FindRevealedAction(MarkPaidAction) ?? FindRevealedAction(MarkPendingAction)
            ?? throw new InvalidOperationException("No entry actions menu is revealed."))
            .FindElement(By.ClassName("android.widget.TextView")).Text;

    /// <summary>
    /// Long-presses the entry and taps "Pagar"/"Receber", then waits for the paid indicator.
    /// </summary>
    public MainPageObject MarkPaid(string description)
    {
        RevealActions(description);
        TapRevealedAction(MarkPaidAction);
        _session.WaitFor(() => IsEntryPaid(description), $"\"{description}\" to show as paid");
        WaitForActionsHidden();
        return this;
    }

    /// <summary>
    /// Long-presses the entry and taps "Pendente", then waits for the paid indicator to disappear.
    /// </summary>
    public MainPageObject MarkPending(string description)
    {
        RevealActions(description);
        TapRevealedAction(MarkPendingAction);
        _session.WaitFor(() => !IsEntryPaid(description), $"\"{description}\" to show as pending");
        WaitForActionsHidden();
        return this;
    }

    public bool IsEntryPaid(string description) =>
        FindRow(description).FindElements(EntryPaidIcon).Any(e => e.Displayed);

    public IReadOnlyList<string> VisibleEntryStatuses => _session.VisibleTexts(EntryStatus);

    public bool IsEntryPartiallyPaid(string description) =>
        FindRow(description).FindElements(EntryPartialIcon).Any(e => e.Displayed);

    /// <summary>
    /// The row's partial payment line (e.g. "Pago R$ 250,00 · falta R$ 550,00"), or null when not partially paid.
    /// </summary>
    public string? EntryPartialText(string description) =>
        FindRow(description).FindElements(EntryPartial).FirstOrDefault(e => e.Displayed)?.Text;

    public string EntryAmountText(string description) =>
        FindRow(description).FindElements(EntryAmount).First(e => e.Displayed).Text;

    public bool IsEntryEstimated(string description) =>
        FindRow(description).FindElements(EntryEstimatedIcon).Any(e => e.Displayed);

    /// <summary>
    /// Opens the entry, enters the amount already paid this month and saves.
    /// </summary>
    public MainPageObject SetPaidAmount(string description, decimal paidAmount)
    {
        OpenEntry(description).EnterPaidAmount(paidAmount).Save();
        return WaitForEntry(description);
    }

    public PngImage CaptureEntryPaidIcon(string description) => CaptureInRow(description, EntryPaidIcon);

    public PngImage CaptureEntryStatusLabel(string description) => CaptureInRow(description, EntryStatus);

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

    public void TapSummaryBar() => _session.Tap(SummaryBar);

    public MonthPickerPopupObject OpenMonthPicker()
    {
        _session.Tap(MonthTitleButton);
        return new MonthPickerPopupObject(_session).WaitUntilOpen();
    }

    public MainPageObject GoToMonth(DateTime month)
    {
        OpenMonthPicker().SelectMonth(month);
        return WaitForMonth(month);
    }

    public MainPageObject WaitForMonth(DateTime month)
    {
        _session.WaitForText(MonthTitle, AppFormat.MonthTitle(month));
        return this;
    }

    public bool IsGoToCurrentMonthVisible => _session.IsVisible(GoToCurrentMonthButton);

    public string GoToCurrentMonthText => _session.WaitForVisible(GoToCurrentMonthButton).Text;

    public MainPageObject TapGoToCurrentMonth()
    {
        _session.Tap(GoToCurrentMonthButton);
        _session.WaitForText(MonthTitle, AppFormat.MonthTitle(AppFormat.CurrentMonth));
        _session.WaitForGone(GoToCurrentMonthButton);
        return this;
    }

    public MainPageObject WaitForGoToCurrentMonth()
    {
        _session.WaitForVisible(GoToCurrentMonthButton);
        return this;
    }

    public MainPageObject GoToNextMonth() => SwipeMonth("left", 1);

    public MainPageObject GoToPreviousMonth() => SwipeMonth("right", -1);

    /// <summary>
    /// A swipe sent while the app is still settling (e.g. right after launch) is sometimes dropped by the
    /// CarouselView. It is retried only when the month provably didn't change, so it can never overshoot.
    /// </summary>
    private MainPageObject SwipeMonth(string direction, int monthOffset)
    {
        const int maxAttempts = 3;
        var original = MonthTitleText;
        var expected = AppFormat.MonthTitle(CurrentMonth.AddMonths(monthOffset));

        for (var attempt = 1; ; attempt++)
        {
            _session.SwipeHorizontally(direction);
            try
            {
                _session.WaitForText(MonthTitle, expected, TimeSpan.FromSeconds(8));
                return this;
            }
            catch (WebDriverTimeoutException) when (attempt < maxAttempts && MonthTitleText == original)
            {
            }
        }
    }

    public MainPageObject TapNextMonthArrow()
    {
        var expected = AppFormat.MonthTitle(CurrentMonth.AddMonths(1));
        _session.Tap(NextMonthButton);
        _session.WaitForText(MonthTitle, expected);
        return this;
    }

    public MainPageObject TapPreviousMonthArrow()
    {
        var expected = AppFormat.MonthTitle(CurrentMonth.AddMonths(-1));
        _session.Tap(PreviousMonthButton);
        _session.WaitForText(MonthTitle, expected);
        return this;
    }

    public bool IsPreviousMonthArrowEnabled => _session.WaitForVisible(PreviousMonthButton).Enabled;

    public bool IsNextMonthArrowEnabled => _session.WaitForVisible(NextMonthButton).Enabled;

    /// <summary>
    /// On-screen bounds of the month title pill, its label and its ▾ badge, for layout assertions.
    /// </summary>
    public (Rectangle Pill, Rectangle Title, Rectangle Badge) MonthHeaderBounds =>
        (Bounds(MonthTitleButton), Bounds(MonthTitle), Bounds(MonthPickerBadge));

    public bool IsSwipeHintVisible => _session.IsVisible(SwipeHint);

    public string SwipeHintText => _session.WaitForVisible(SwipeHint).Text;

    public MainPageObject WaitForSwipeHintGone()
    {
        _session.WaitForGone(SwipeHint);
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

    private void TapRevealedAction(By action) =>
        (FindRevealedAction(action) ?? throw new InvalidOperationException($"No revealed {action} in the entry actions menu.")).Click();

    private IWebElement? FindRevealedAction(By action)
    {
        var rows = _session.Driver.FindElements(EntryRow).Where(r => r.Displayed).ToList();
        if (rows.Count == 0)
            return null;

        var rowRight = rows.Max(r => r.Location.X + r.Size.Width);
        return _session.Driver.FindElements(action)
            .FirstOrDefault(a => a.Displayed && a.Location.X + a.Size.Width / 2 < rowRight);
    }

    private IWebElement FindRow(string description)
    {
        _session.WaitForVisible(EntryByDescription(description));
        return _session.Driver.FindElements(EntryRow)
            .First(r => r.Displayed && r.FindElements(EntryDescription).Any(d => d.Text == description));
    }

    /// <summary>
    /// The element is first scrolled above the floating buttons at the bottom, which would otherwise tint its pixels.
    /// </summary>
    private PngImage CaptureInRow(string description, By element)
    {
        IWebElement Find() => FindRow(description).FindElements(element).First(e => e.Displayed);

        _session.ScrollAbove(Find, _session.WaitForVisible(AddEntryButton).Location.Y);
        return AppSession.Capture(Find());
    }

    private Rectangle Bounds(By locator)
    {
        var element = _session.WaitForVisible(locator);
        return new Rectangle(element.Location, element.Size);
    }

    private static By EntryByDescription(string description) =>
        AppSession.AutomationIdWithText("EntryDescriptionLabel", description);
}
