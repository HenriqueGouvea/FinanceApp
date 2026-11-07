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
    private static readonly By EntryPaymentSource = AppSession.AutomationId("EntryPaymentSourceLabel");
    private static readonly By EntryInstallment = AppSession.AutomationId("EntryInstallmentLabel");
    private static readonly By EntryInstallmentTotal = AppSession.AutomationId("EntryInstallmentTotalLabel");
    private static readonly By CompactMonthButton = AppSession.AutomationId("CompactMonthButton");
    private static readonly By CompactMonthTitle = AppSession.AutomationId("CompactMonthTitleLabel");
    private static readonly By HeaderAvatarInitial = AppSession.AutomationId("HeaderAvatarInitialLabel");
    private static readonly By EntryDueDate = AppSession.AutomationId("EntryDueDateLabel");
    private static readonly By DeleteAction = AppSession.AutomationId("DeleteEntryAction");
    private static readonly By MarkPaidAction = AppSession.AutomationId("MarkPaidEntryAction");
    private static readonly By MarkPendingAction = AppSession.AutomationId("MarkPendingEntryAction");
    private static readonly By EntryPaidIcon = AppSession.AutomationId("EntryPaidIcon");
    private static readonly By EntryStatus = AppSession.AutomationId("EntryStatusLabel");
    private static readonly By EntryPartialIcon = AppSession.AutomationId("EntryPartialIcon");
    private static readonly By EntryPartial = AppSession.AutomationId("EntryPartialLabel");
    private static readonly By EntryEstimatedIcon = AppSession.AutomationId("EntryEstimatedIcon");
    private static readonly By EntryPlannedAmount = AppSession.AutomationId("EntryPlannedAmountLabel");
    private static readonly By EntryPaymentProgress = AppSession.AutomationId("EntryPaymentProgress");
    private static readonly By PartialPaymentAction = AppSession.AutomationId("PartialPaymentEntryAction");
    private static readonly By PagerDot = AppSession.AutomationId("PagerDot");
    private static readonly By MonthTitleButton = AppSession.AutomationId("MonthTitleButton");
    private static readonly By SummaryBar = AppSession.AutomationId("SummaryBar");
    private static readonly By GoToCurrentMonthButton = AppSession.AutomationId("GoToCurrentMonthButton");
    private static readonly By PreviousMonthButton = AppSession.AutomationId("PreviousMonthButton");
    private static readonly By NextMonthButton = AppSession.AutomationId("NextMonthButton");
    private static readonly By SwipeHint = AppSession.AutomationId("SwipeHintLabel");
    private static readonly By MonthPickerBadge = AppSession.AutomationId("MonthPickerBadge");
    private static readonly By FlyoutGreeting = AppSession.AutomationId("FlyoutGreetingLabel");
    private static readonly By FlyoutSourceRow = AppSession.AutomationId("FlyoutSourceRow");
    private static readonly By FlyoutSourceName = AppSession.AutomationId("FlyoutSourceNameLabel");
    private static readonly By FlyoutSourceBalance = AppSession.AutomationId("FlyoutSourceBalanceLabel");
    private static readonly By Bill = AppSession.AutomationId("CreditCardBill");
    private static readonly By BillRow = AppSession.AutomationId("CreditCardBillRow");
    private static readonly By BillName = AppSession.AutomationId("CreditCardBillNameLabel");
    private static readonly By BillAmount = AppSession.AutomationId("CreditCardBillAmountLabel");
    private static readonly By BillDueDate = AppSession.AutomationId("CreditCardBillDueDateLabel");
    private static readonly By BillAccount = AppSession.AutomationId("CreditCardBillAccountLabel");
    private static readonly By BillCount = AppSession.AutomationId("CreditCardBillCountLabel");
    private static readonly By BillStatus = AppSession.AutomationId("CreditCardBillStatusLabel");
    private static readonly By BillPaidIcon = AppSession.AutomationId("CreditCardBillPaidIcon");
    private static readonly By BillEntries = AppSession.AutomationId("CreditCardBillEntries");
    private static readonly By PayBillAction = AppSession.AutomationId("PayCreditCardBillAction");
    private static readonly By MarkBillPendingAction = AppSession.AutomationId("MarkCreditCardBillPendingAction");
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

    /// <summary>
    /// Waits for "Carregando lançamentos..." to go away: the month on screen has its entries.
    /// </summary>
    public MainPageObject WaitForMonthLoaded()
    {
        _session.WaitForGone(AppSession.AutomationId("MonthLoadingIndicator"));
        return this;
    }

    public string MonthTitleText => _session.WaitForVisible(MonthTitle).Text;
    public string SummaryIncomeText => SummaryValue(SummaryPage.Totals, SummaryIncome);
    public string SummaryOutcomeText => SummaryValue(SummaryPage.Totals, SummaryOutcome);
    public string SummaryBalanceText => SummaryValue(SummaryPage.Totals, SummaryBalance);
    public string SummaryPaidText => SummaryValue(SummaryPage.Paid, SummaryPaid);
    public string SummaryToPayText => SummaryValue(SummaryPage.Paid, SummaryToPay);
    public string SummaryReceivedText => SummaryValue(SummaryPage.Received, SummaryReceived);
    public string SummaryToReceiveText => SummaryValue(SummaryPage.Received, SummaryToReceive);

    /// <summary>
    /// The summary page currently shown, or null while none is fully displayed (e.g. mid-animation).
    /// </summary>
    public SummaryPage? VisibleSummaryPage =>
        Enum.GetValues<SummaryPage>().Cast<SummaryPage?>().FirstOrDefault(p => _session.IsVisible(SummaryPageLocator(p!.Value)));

    public MainPageObject SwipeSummary(string direction)
    {
        _session.SwipeOn(_session.WaitForVisible(SummaryBar), direction);
        return this;
    }

    public MainPageObject WaitForSummaryPage(SummaryPage page)
    {
        _session.WaitFor(() => VisibleSummaryPage == page, $"the {page} summary page to show");
        return this;
    }

    /// <summary>
    /// Taps the page's dot below the summary (dots are ordered left to right like the pages).
    /// </summary>
    public MainPageObject TapSummaryDot(SummaryPage page)
    {
        _session.WaitForVisible(PagerDot);
        _session.Driver.FindElements(PagerDot).Where(d => d.Displayed).OrderBy(d => d.Location.X).ElementAt((int)page).Click();
        return WaitForSummaryPage(page);
    }

    private string SummaryValue(SummaryPage page, By value)
    {
        if (VisibleSummaryPage != page)
            TapSummaryDot(page);
        return _session.WaitForVisible(value).Text;
    }

    private static By SummaryPageLocator(SummaryPage page) => AppSession.AutomationId(page switch
    {
        SummaryPage.Totals => "SummaryTotalsPage",
        SummaryPage.Paid => "SummaryPaidPage",
        _ => "SummaryReceivedPage"
    });
    public string OutcomeCountText => _session.WaitForVisible(OutcomeCount).Text;
    public string OutcomeTotalText => _session.WaitForVisible(OutcomeTotal).Text;
    public string IncomeCountText => _session.WaitForVisible(IncomeCount).Text;
    public string IncomeTotalText => _session.WaitForVisible(IncomeTotal).Text;

    public IReadOnlyList<string> VisibleEntryDescriptions => _session.VisibleTexts(EntryDescription);
    public IReadOnlyList<string> VisibleEntryAmounts => _session.VisibleTexts(EntryAmount);
    public IReadOnlyList<string> VisibleEntryCategories => _session.VisibleTexts(EntryCategory);
    public IReadOnlyList<string> VisibleEntryPaymentSources => _session.VisibleTexts(EntryPaymentSource);
    /// <summary>
    /// The installment badges beside the descriptions, e.g. "1/3".
    /// </summary>
    public IReadOnlyList<string> VisibleInstallmentLabels => _session.VisibleTexts(EntryInstallment);

    /// <summary>
    /// The installment totals in the rows' details, e.g. "total R$ 300,00".
    /// </summary>
    public IReadOnlyList<string> VisibleInstallmentTotals => _session.VisibleTexts(EntryInstallmentTotal);

    public bool IsEntryVisible(string description) => _session.IsVisible(EntryByDescription(description));

    /// <summary>
    /// Waits for a state that the page reaches asynchronously, e.g. after a save refreshes the month.
    /// </summary>
    public MainPageObject WaitFor(Func<bool> condition, [CallerArgumentExpression(nameof(condition))] string description = "")
    {
        _session.WaitFor(condition, description);
        return this;
    }

    public AddEntryPageObject OpenAddEntry()
    {
        _session.Tap(AddEntryButton);
        return new AddEntryPageObject(_session).WaitUntilOpen();
    }

    public MainPageObject AddEntry(
        string description,
        string recurrence,
        decimal amount,
        bool isIncome = false,
        string? installments = null,
        string? category = null,
        bool isEstimated = false,
        bool allowsPartialPayment = false,
        int? dueDay = null,
        string? paymentSource = null)
    {
        var entryPage = OpenAddEntry().EnterDescription(description);

        if (isIncome)
            entryPage.SelectIncome();

        entryPage.SelectRecurrence(recurrence);

        if (installments is not null)
            entryPage.EnterInstallments(installments);

        entryPage.EnterAmount(amount);

        if (isEstimated)
            entryPage.SetEstimated(true);

        if (allowsPartialPayment)
            entryPage.SetAllowsPartialPayment(true);

        if (dueDay is not null)
            entryPage.SelectDueDay(dueDay);

        if (category is not null)
            entryPage.SelectCategory(category);

        if (paymentSource is not null)
            entryPage.SelectPaymentSource(paymentSource);

        entryPage.Save();
        return WaitForEntry(description);
    }

    public AddEntryPageObject OpenEntry(string description)
    {
        _session.Tap(EntryByDescription(description));
        return new AddEntryPageObject(_session).WaitUntilOpen();
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
    /// The amount paid so far (e.g. "R$ 250,00"), which the row shows as its amount while partially paid within the
    /// planned amount; null when not partially paid or already beyond the planned amount.
    /// </summary>
    public string? EntryPaidSoFarText(string description)
    {
        var row = FindRow(description);
        return row.FindElements(EntryPartial).Any(e => e.Displayed) ? row.FindElements(EntryAmount).First(e => e.Displayed).Text : null;
    }

    /// <summary>
    /// The "de R$ 800,00" line under the amount paid so far of a partially paid entry, or null otherwise.
    /// </summary>
    public string? EntryOfPlannedText(string description) =>
        FindRow(description).FindElements(EntryPartial).FirstOrDefault(e => e.Displayed)?.Text;

    public string EntryAmountText(string description) =>
        FindRow(description).FindElements(EntryAmount).First(e => e.Displayed).Text;

    /// <summary>
    /// The "▲ R$ 800,00" line (planned amount, struck through) under the price of an entry paid beyond it, or null otherwise.
    /// </summary>
    public string? EntryPlannedAmountText(string description) =>
        FindRow(description).FindElements(EntryPlannedAmount).FirstOrDefault(e => e.Displayed)?.Text;

    /// <summary>
    /// Whether the payment progress bar shows under the row's price.
    /// </summary>
    public bool IsEntryPaymentProgressVisible(string description) =>
        FindRow(description).FindElements(EntryPaymentProgress).Any(e => e.Displayed);

    /// <summary>
    /// The due date line under the description ("Vence hoje" or e.g. "Vence 05/11"), or null when the entry has no due date.
    /// </summary>
    public string? EntryDueDateText(string description) =>
        FindRow(description).FindElements(EntryDueDate).FirstOrDefault(e => e.Displayed)?.Text;

    public string EntryEstimatedSymbol(string description) =>
        FindRow(description).FindElements(EntryEstimatedIcon).First(e => e.Displayed)
            .FindElement(By.ClassName("android.widget.TextView")).Text;

    /// <summary>
    /// Text of the partial payment action in the revealed menu ("Pagar parcial"/"Receber parcial"), or null when it isn't offered.
    /// </summary>
    public string? RevealedPartialPaymentActionText =>
        FindRevealedAction(PartialPaymentAction)?.FindElement(By.ClassName("android.widget.TextView")).Text;

    /// <summary>
    /// Long-presses the entry and taps "Pagar parcial"/"Receber parcial".
    /// </summary>
    public PartialPaymentPopupObject OpenPartialPayment(string description)
    {
        RevealActions(description);
        TapRevealedAction(PartialPaymentAction);
        return new PartialPaymentPopupObject(_session).WaitUntilOpen();
    }

    public bool IsEntryEstimated(string description) =>
        FindRow(description).FindElements(EntryEstimatedIcon).Any(e => e.Displayed);

    /// <summary>
    /// The row's background color, sampled at its top-left corner, inside the padding where no content is drawn.
    /// </summary>
    public (byte R, byte G, byte B) EntryRowBackground(string description)
    {
        IWebElement Find() => FindRow(description);

        _session.ScrollAbove(Find, _session.WaitForVisible(AddEntryButton).Location.Y);

        // Without UiAutomator2's idle wait the list may still be gliding, and the capture would catch a neighbouring row.
        var last = Point.Empty;
        _session.WaitFor(() =>
        {
            var current = Find().Location;
            var settled = current == last;
            last = current;
            return settled;
        }, $"the \"{description}\" row to stop moving");

        return AppSession.Capture(Find()).GetPixel(3, 3);
    }

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

    /// <summary>
    /// The card under the sections telling how to expand a collapsed one, or null while both are expanded.
    /// </summary>
    public string? CollapsedSectionsHintText =>
        _session.TryScrollTo("CollapsedSectionsHintLabel") is { Displayed: true } hint ? hint.Text : null;

    /// <summary>
    /// Scrolls the month down until its header leaves the screen, then reads the month pill the top bar shows instead.
    /// </summary>
    public string ScrollToCompactMonthTitle()
    {
        _session.ScrollDownUntil(() => _session.IsVisible(CompactMonthTitle), "the month to show in the top bar");
        return _session.WaitForVisible(CompactMonthTitle).Text;
    }

    /// <summary>
    /// Taps the month pill in the top bar (shown once the month header has scrolled away), which opens the month picker.
    /// </summary>
    public MonthPickerPopupObject OpenMonthPickerFromTopBar()
    {
        _session.Tap(CompactMonthButton);
        return new MonthPickerPopupObject(_session).WaitUntilOpen();
    }

    /// <summary>
    /// The user's initial in the top bar's avatar.
    /// </summary>
    public string HeaderAvatarInitialText => _session.WaitForVisible(HeaderAvatarInitial).Text;

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

    /// <summary>
    /// Waits for an entry row to show that payment source, e.g. after the source is renamed.
    /// </summary>
    public MainPageObject WaitForEntryPaymentSource(string sourceName)
    {
        _session.WaitForVisible(AppSession.AutomationIdWithText("EntryPaymentSourceLabel", sourceName));
        return this;
    }

    public PaymentSourcesPageObject OpenPaymentSources()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.PaymentSourcesMenuItem));
        return new PaymentSourcesPageObject(_session).WaitUntilLoaded();
    }

    public SettingsPageObject OpenSettings()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.SettingsMenuItem));
        return new SettingsPageObject(_session).WaitUntilLoaded();
    }

    /// <summary>
    /// "PIN e biometria" in the settings.
    /// </summary>
    public SecurityPageObject OpenSecurity() => OpenSettings().OpenSecurity();

    /// <summary>
    /// Opens the hamburger menu, reads its greeting (e.g. "Olá, Ana!") and closes it again.
    /// </summary>
    public string ReadMenuGreeting()
    {
        _session.Tap(FlyoutButton);
        var greeting = _session.WaitForVisible(FlyoutGreeting).Text;
        _session.PressBack();
        _session.WaitForGone(FlyoutGreeting);
        return greeting;
    }

    /// <summary>
    /// Opens the hamburger menu, waits for the source's amount in "Saldo atual" (the available limit for a
    /// credit card) to read <paramref name="expected"/>, and closes it by tapping the dimmed page beside it (Back would leave the app). The list reloads as the menu opens.
    /// </summary>
    public MainPageObject WaitForMenuBalance(string sourceName, string expected)
    {
        _session.Tap(FlyoutButton);
        _session.WaitFor(() => ReadMenuBalance(sourceName) == expected, $"\"{sourceName}\" to show {expected} in the menu");
        _session.TapAt(0.92, 0.5);
        _session.WaitForGone(FlyoutGreeting);
        WaitUntilLoaded();
        return this;
    }

    private string? ReadMenuBalance(string sourceName)
    {
        try
        {
            return _session.Driver.FindElements(FlyoutSourceRow)
                .FirstOrDefault(r => r.Displayed && r.FindElements(FlyoutSourceName).Any(n => n.Text == sourceName))
                ?.FindElement(FlyoutSourceBalance).Text;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    /// <summary>
    /// Opens the setup wizard again through "Refazer configuração inicial" in the settings.
    /// </summary>
    public SetupPageObject RedoSetup() => OpenSettings().RedoSetup();

    public bool IsLoaded => _session.IsVisible(AddEntryButton);

    // --- Credit card bills ---

    /// <summary>
    /// The bill rows of the displayed month, e.g. "💳 Fatura Nubank" (see <see cref="AppLabels.BillName"/>).
    /// </summary>
    public IReadOnlyList<string> VisibleBillNames => _session.VisibleTexts(BillName);

    public MainPageObject WaitForBill(string card)
    {
        _session.WaitForVisible(BillByCard(card));
        return this;
    }

    public string BillAmountText(string card) => FindBillRow(card).FindElement(BillAmount).Text;

    public string BillCountText(string card) => FindBillRow(card).FindElement(BillCount).Text;

    /// <summary>
    /// The bill's due date line ("Vence hoje" or e.g. "Vence 10/11"), or null when the card has no due day.
    /// </summary>
    public string? BillDueDateText(string card) => VisibleTextIn(FindBillRow(card), BillDueDate);

    /// <summary>
    /// The "Débito em Itaú" line, or null when no account pays the bill.
    /// </summary>
    public string? BillAccountText(string card) => VisibleTextIn(FindBillRow(card), BillAccount);

    public bool IsBillPaid(string card) =>
        FindBillRow(card).FindElements(BillPaidIcon).Any(e => e.Displayed) && VisibleTextIn(FindBillRow(card), BillStatus) == AppLabels.BillPaid;

    /// <summary>
    /// The descriptions of the entries listed under the card's bill (empty while it is collapsed).
    /// </summary>
    public IReadOnlyList<string> BillEntryDescriptions(string card) =>
        FindBill(card).FindElements(BillEntries).Where(e => e.Displayed)
            .SelectMany(e => e.FindElements(EntryDescription))
            .Where(d => d.Displayed)
            .Select(d => d.Text)
            .ToList();

    /// <summary>
    /// Taps the bill row to collapse or expand its entries.
    /// </summary>
    public MainPageObject ToggleBill(string card)
    {
        var expanded = BillEntryDescriptions(card).Count > 0;
        FindBillRow(card).Click();
        _session.WaitFor(() => BillEntryDescriptions(card).Count > 0 != expanded, $"the \"{card}\" bill to {(expanded ? "collapse" : "expand")}");
        return this;
    }

    public MainPageObject RevealBillActions(string card)
    {
        _session.LongPress(FindBillRow(card));
        _session.WaitFor(() => FindRevealedAction(PayBillAction, BillRow) is not null || FindRevealedAction(MarkBillPendingAction, BillRow) is not null,
            "the bill actions menu to slide in");
        return this;
    }

    /// <summary>
    /// Text of the status action in the revealed bill menu ("Pagar fatura" or "Pendente").
    /// </summary>
    public string RevealedBillActionText =>
        (FindRevealedAction(PayBillAction, BillRow) ?? FindRevealedAction(MarkBillPendingAction, BillRow)
            ?? throw new InvalidOperationException("No bill actions menu is revealed."))
            .FindElement(By.ClassName("android.widget.TextView")).Text;

    /// <summary>
    /// Long-presses the bill and taps "Pagar fatura", then waits for the bill to show as paid.
    /// </summary>
    public MainPageObject PayBill(string card)
    {
        RevealBillActions(card);
        TapRevealedAction(PayBillAction, BillRow);
        _session.WaitFor(() => IsBillPaid(card), $"the \"{card}\" bill to show as paid");
        return this;
    }

    /// <summary>
    /// Long-presses a paid bill and taps "Pendente", then waits for the paid indicator to disappear.
    /// </summary>
    public MainPageObject MarkBillPending(string card)
    {
        RevealBillActions(card);
        TapRevealedAction(MarkBillPendingAction, BillRow);
        _session.WaitFor(() => !IsBillPaid(card), $"the \"{card}\" bill to show as pending");
        return this;
    }

    private IWebElement FindBill(string card)
    {
        _session.WaitForVisible(BillByCard(card));
        return _session.Driver.FindElements(Bill)
            .First(b => b.Displayed && b.FindElements(BillName).Any(n => n.Displayed && n.Text == AppLabels.BillName(card)));
    }

    private IWebElement FindBillRow(string card) => FindBill(card).FindElement(BillRow);

    private static string? VisibleTextIn(IWebElement container, By locator) =>
        container.FindElements(locator).FirstOrDefault(e => e.Displayed)?.Text;

    private static By BillByCard(string card) =>
        AppSession.AutomationIdWithText("CreditCardBillNameLabel", AppLabels.BillName(card));

    private void TapRevealedAction(By action, By? row = null) =>
        (FindRevealedAction(action, row) ?? throw new InvalidOperationException($"No revealed {action} in the actions menu.")).Click();

    private IWebElement? FindRevealedAction(By action, By? row = null)
    {
        var rows = _session.Driver.FindElements(row ?? EntryRow).Where(r => r.Displayed).ToList();
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

    private Rectangle Bounds(By locator)
    {
        var element = _session.WaitForVisible(locator);
        return new Rectangle(element.Location, element.Size);
    }

    private static By EntryByDescription(string description) =>
        AppSession.AutomationIdWithText("EntryDescriptionLabel", description);
}

/// <summary>
/// Pages of the month summary, in swipe order.
/// </summary>
public enum SummaryPage { Totals = 0, Paid = 1, Received = 2 }
