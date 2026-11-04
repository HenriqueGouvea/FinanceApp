using System.Globalization;
using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// The setup wizard shown on the first run and from the menu's "Refazer configuração inicial".
/// </summary>
public class SetupPageObject
{
    private const string SalaryEntryId = "SetupSalaryEntry";
    private const string SalaryDayPickerId = "SetupSalaryDayPicker";
    private const string ExpensesSourcePickerId = "SetupExpensesSourcePicker";

    private static readonly By StepLabel = AppSession.AutomationId("SetupStepLabel");
    private static readonly By SkipButton = AppSession.AutomationId("SetupSkipButton");
    private static readonly By BackButton = AppSession.AutomationId("SetupBackButton");
    private static readonly By NextButton = AppSession.AutomationId("SetupNextButton");
    private static readonly By WelcomeStep = AppSession.AutomationId("SetupWelcomeStep");
    private static readonly By WelcomeTitle = AppSession.AutomationId("SetupWelcomeTitleLabel");
    private static readonly By Tagline = AppSession.AutomationId("SetupTaglineLabel");
    private static readonly By NameEntry = AppSession.AutomationId("SetupNameEntry");
    private static readonly By AvatarInitial = AppSession.AutomationId("SetupAvatarInitialLabel");
    private static readonly By SourceBalanceEntry = AppSession.AutomationId("SetupSourceBalanceEntry");
    private static readonly By SourceBalanceCaption = AppSession.AutomationId("SetupSourceBalanceCaption");
    private static readonly By SourceName = AppSession.AutomationId("SetupSourceNameLabel");
    private static readonly By SummaryTitle = AppSession.AutomationId("SetupSummaryTitleLabel");
    private static readonly By SummaryRow = AppSession.AutomationId("SetupSummaryRow");
    private static readonly By SummaryDescription = AppSession.AutomationId("SetupSummaryDescriptionLabel");
    private static readonly By SummaryDetail = AppSession.AutomationId("SetupSummaryDetailLabel");
    private static readonly By SummaryAmount = AppSession.AutomationId("SetupSummaryAmountLabel");

    private readonly AppSession _session;

    public SetupPageObject(AppSession session)
    {
        _session = session;
    }

    /// <summary>
    /// On the first run the wizard is the first screen of a cold start, so it gets the same time to show up as the entries page.
    /// </summary>
    public SetupPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(StepLabel, TimeSpan.FromSeconds(60));
        return this;
    }

    /// <summary>
    /// "Passo 1 de 5", see <see cref="AppFormat.SetupStep"/>.
    /// </summary>
    public string StepText => _session.WaitForVisible(StepLabel).Text;

    public SetupPageObject WaitForStep(int step)
    {
        _session.WaitForText(StepLabel, AppFormat.SetupStep(step));
        return this;
    }

    public string SkipButtonText => _session.WaitForVisible(SkipButton).Text;

    public bool IsBackVisible => _session.IsVisible(BackButton);

    public bool IsWelcomeStepVisible => _session.IsVisible(WelcomeStep);

    public string WelcomeTitleText => _session.WaitForVisible(WelcomeTitle).Text;

    public string TaglineText => _session.WaitForVisible(Tagline).Text;

    public string NextButtonText => _session.WaitForVisible(NextButton).Text;

    public SetupPageObject Next()
    {
        var step = CurrentStep;
        _session.HideKeyboardIfShown();
        _session.Tap(NextButton);
        return WaitForStep(step + 1);
    }

    public SetupPageObject Back()
    {
        var step = CurrentStep;
        _session.HideKeyboardIfShown();
        _session.Tap(BackButton);
        return WaitForStep(step - 1);
    }

    /// <summary>
    /// Android's back button, which goes to the previous step.
    /// </summary>
    public SetupPageObject PressBack()
    {
        var step = CurrentStep;
        _session.HideKeyboardIfShown();
        _session.PressBack();
        return WaitForStep(step - 1);
    }

    /// <summary>
    /// Taps "Pular" on the first run, which skips the wizard for good without creating anything.
    /// </summary>
    public MainPageObject Skip()
    {
        TapSkip();
        return new MainPageObject(_session).WaitUntilLoaded();
    }

    /// <summary>
    /// Taps "Fechar" on a wizard opened from the settings, which returns there without creating anything.
    /// </summary>
    public SettingsPageObject Close()
    {
        TapSkip();
        return new SettingsPageObject(_session).WaitUntilLoaded();
    }

    private void TapSkip()
    {
        _session.HideKeyboardIfShown();
        _session.Tap(SkipButton);
        _session.WaitForGone(StepLabel);
    }

    /// <summary>
    /// Taps "Começar" on the summary, which creates everything and closes the wizard.
    /// </summary>
    public MainPageObject Finish()
    {
        _session.Tap(NextButton);
        _session.WaitForGone(StepLabel);
        return new MainPageObject(_session).WaitUntilLoaded();
    }

    // --- 1. Welcome ---

    public string NameText => _session.WaitForVisible(NameEntry).Text;

    public string AvatarInitialText => _session.WaitForVisible(AvatarInitial).Text;

    public SetupPageObject EnterName(string name)
    {
        var entry = _session.WaitForVisible(NameEntry);
        entry.Clear();
        entry.SendKeys(name);
        _session.HideKeyboardIfShown();
        return this;
    }

    // --- 2. Accounts and cards ---

    /// <summary>
    /// Fills the form with the source's name, type (<see cref="AppFormat.BankAccount"/>, ...) and balance, then taps "Adicionar".
    /// </summary>
    public SetupPageObject AddSource(string name, int type, decimal balance)
    {
        var count = SourceNames.Count;

        var nameEntry = _session.ScrollTo("SetupSourceNameEntry");
        nameEntry.Clear();
        nameEntry.SendKeys(name);
        _session.HideKeyboardIfShown();

        _session.ScrollToAndTap(type switch
        {
            AppFormat.CreditCard => "SetupSourceCreditCardTypeButton",
            AppFormat.MealVoucher => "SetupSourceMealVoucherTypeButton",
            _ => "SetupSourceBankAccountTypeButton"
        });
        _session.WaitForText(SourceBalanceCaption, type == AppFormat.CreditCard ? AppLabels.Limit : AppLabels.Balance);

        TypeAmount(_session.ScrollTo("SetupSourceBalanceEntry"), SourceBalanceEntry, balance);

        _session.ScrollToAndTap("SetupAddSourceButton");
        _session.WaitFor(() => SourceNames.Count == count + 1, $"\"{name}\" to be added");
        return this;
    }

    public IReadOnlyList<string> SourceNames => _session.VisibleTexts(SourceName);

    // --- 3. Income ---

    public SetupPageObject EnterSalary(decimal amount)
    {
        TypeAmount(_session.ScrollTo(SalaryEntryId), AppSession.AutomationId(SalaryEntryId), amount);
        return this;
    }

    public SetupPageObject SelectSalaryDay(int day)
    {
        _session.ScrollToAndTap(SalaryDayPickerId);
        _session.ScrollDialogListToAndTap(AppLabels.DueDay(day));
        _session.WaitForText(AppSession.AutomationId(SalaryDayPickerId), AppLabels.DueDay(day));
        return this;
    }

    // --- 4. House expenses ---

    /// <summary>
    /// Ticks the expense and fills its monthly amount and, when given, its due day.
    /// </summary>
    public SetupPageObject SelectExpense(string description, decimal amount, int? dueDay = null)
    {
        ToggleExpense(description);
        if (!IsExpenseChecked(description))
            throw new InvalidOperationException($"\"{description}\" was already selected.");

        // Ticking the row reveals its amount and due day fields, which may open below the fold.
        var amountEntry = ExpenseRowElement(description, "SetupExpenseAmountEntry");
        var footerTop = _session.WaitForVisible(NextButton).Location.Y;
        _session.ScrollDownUntil(() => IsAboveFooter(amountEntry, footerTop), $"the amount of \"{description}\" to be on screen");
        TypeAmount(_session.WaitForVisible(amountEntry), amountEntry, amount);

        if (dueDay is { } day)
        {
            var picker = ExpenseRowElement(description, "SetupExpenseDueDayPicker");
            _session.Tap(picker);
            _session.ScrollDialogListToAndTap(AppLabels.DueDay(day));
            _session.WaitForText(picker, AppLabels.DueDay(day));
        }

        return this;
    }

    /// <summary>
    /// Taps the expense's row, which ticks or unticks it.
    /// </summary>
    public SetupPageObject ToggleExpense(string description)
    {
        _session.HideKeyboardIfShown();
        ScrollToText(description);
        var wasChecked = IsExpenseChecked(description);
        _session.Tap(ExpenseRowElement(description, "SetupExpenseNameLabel"));
        _session.WaitFor(() => IsExpenseChecked(description) != wasChecked, $"\"{description}\" to be toggled");
        return this;
    }

    public bool IsExpenseChecked(string description) =>
        _session.WaitForVisible(ExpenseRowElement(description, "SetupExpenseCheckBox")).GetAttribute("checked") == "true";

    // The options sit at the bottom of the row, below the fold once the keyboard closes, so they are scrolled clear
    // of the footer buttons first. Expenses are handled top to bottom, so scrolling down is enough.
    private void ShowOption(string description, string optionId)
    {
        _session.HideKeyboardIfShown();
        var option = ExpenseRowElement(description, optionId);
        var footerTop = _session.WaitForVisible(NextButton).Location.Y;
        _session.ScrollDownUntil(() => IsAboveFooter(option, footerTop), $"the option of \"{description}\" to be on screen");
    }

    private bool IsOptionChecked(string description, string checkBoxId) =>
        _session.WaitForVisible(ExpenseRowElement(description, checkBoxId)).GetAttribute("checked") == "true";

    public SetupPageObject SelectExpensesSource(string sourceName)
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap(ExpensesSourcePickerId);
        _session.Tap(AppSession.Text(sourceName));
        _session.WaitForText(AppSession.AutomationId(ExpensesSourcePickerId), sourceName);
        return this;
    }

    // --- 5. Summary ---

    public string SummaryTitleText => _session.WaitForVisible(SummaryTitle).Text;

    /// <summary>
    /// The entries listed on the summary: description, detail (e.g. "Todo mês · dia 5 · Nubank") and amount.
    /// </summary>
    public IReadOnlyList<(string Description, string Detail, string Amount)> SummaryRows =>
        _session.Driver.FindElements(SummaryRow)
            .Where(r => r.Displayed)
            .Select(r => (r.FindElement(SummaryDescription).Text, r.FindElement(SummaryDetail).Text, r.FindElement(SummaryAmount).Text))
            .ToList();

    public string SummaryIncomeText => _session.ScrollTo("SetupSummaryIncomeLabel").Text;
    public string SummaryOutcomeText => _session.ScrollTo("SetupSummaryOutcomeLabel").Text;
    public string SummaryBalanceText => _session.ScrollTo("SetupSummaryBalanceLabel").Text;

    public bool IsRecurrenceGuideVisible => _session.TryScrollTo("SetupRecurrenceGuide") is not null;

    /// <summary>
    /// Masked as currency like every amount field: typed digits are read as cents.
    /// </summary>
    private void TypeAmount(IWebElement entry, By locator, decimal amount)
    {
        entry.Clear();
        entry.SendKeys(decimal.ToInt64(amount * 100).ToString(CultureInfo.InvariantCulture));
        _session.WaitForText(locator, AppFormat.Money(amount));
        _session.HideKeyboardIfShown();
    }

    private bool IsAboveFooter(By locator, int footerTop) =>
        _session.Driver.FindElements(locator).Any(e => e.Displayed && e.Location.Y + e.Size.Height <= footerTop);

    private int CurrentStep => int.Parse(StepText.Split(' ')[1], CultureInfo.InvariantCulture);

    private void ScrollToText(string text) =>
        _session.WaitForVisible(MobileBy.AndroidUIAutomator(
            $"new UiScrollable(new UiSelector().scrollable(true).packageName(\"{E2ESettings.AppPackage}\")).scrollIntoView(new UiSelector().text(\"{text}\"))"));

    private static By ExpenseRowElement(string description, string automationId) =>
        By.XPath($"//*[@resource-id='{E2ESettings.AppPackage}:id/SetupExpenseRow'][.//*[@text='{description}']]" +
                 $"//*[@resource-id='{E2ESettings.AppPackage}:id/{automationId}']");
}
