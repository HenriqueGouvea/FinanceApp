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

    private static readonly By StepLabel = AppSession.AutomationId("SetupStepLabel");
    private static readonly By SkipButton = AppSession.AutomationId("SetupSkipButton");
    private static readonly By BackButton = AppSession.AutomationId("SetupBackButton");
    private static readonly By NextButton = AppSession.AutomationId("SetupNextButton");
    private static readonly By WelcomeStep = AppSession.AutomationId("SetupWelcomeStep");
    private static readonly By WelcomeTitle = AppSession.AutomationId("SetupWelcomeTitleLabel");
    private static readonly By Tagline = AppSession.AutomationId("SetupTaglineLabel");
    private static readonly By AvatarInitial = AppSession.AutomationId("SetupAvatarInitialLabel");
    private static readonly By SourceBalanceEntry = AppSession.AutomationId("SetupSourceBalanceEntry");
    private static readonly By SourceBalanceCaption = AppSession.AutomationId("SetupSourceBalanceCaption");
    private static readonly By SourceName = AppSession.AutomationId("SetupSourceNameLabel");
    private static readonly By CardName = AppSession.AutomationId("SetupCardNameLabel");
    private static readonly By CardDetail = AppSession.AutomationId("SetupCardDetailLabel");
    private static readonly By PurchaseDescription = AppSession.AutomationId("SetupPurchaseDescriptionLabel");
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

    public string NameText => _session.ScrollTo("SetupNameEntry").Text;

    public string AvatarInitialText => _session.WaitForVisible(AvatarInitial).Text;

    public SetupPageObject EnterName(string name)
    {
        var entry = _session.ScrollTo("SetupNameEntry");
        entry.Clear();
        entry.SendKeys(name);
        _session.HideKeyboardIfShown();
        return this;
    }

    // --- 2. Accounts and vouchers ---

    /// <summary>
    /// Fills the form with the source's name, type (<see cref="AppFormat.BankAccount"/> or <see cref="AppFormat.MealVoucher"/>)
    /// and balance, then taps "Adicionar". Credit cards have a step of their own, see <see cref="AddCard"/>.
    /// </summary>
    public SetupPageObject AddSource(string name, int type, decimal balance)
    {
        var count = SourceNames.Count;

        TypeSourceName(name);

        _session.ScrollToAndTap(type == AppFormat.MealVoucher ? "SetupSourceMealVoucherTypeButton" : "SetupSourceBankAccountTypeButton");
        _session.WaitForText(SourceBalanceCaption, AppLabels.Balance);

        TypeAmount(_session.ScrollTo("SetupSourceBalanceEntry"), SourceBalanceEntry, balance);

        _session.ScrollToAndTap("SetupAddSourceButton");
        _session.WaitFor(() => SourceNames.Count == count + 1, $"\"{name}\" to be added");
        return this;
    }

    /// <summary>
    /// Types the new source's name without tapping "Adicionar".
    /// </summary>
    public SetupPageObject TypeSourceName(string name)
    {
        var nameEntry = _session.ScrollTo("SetupSourceNameEntry");
        nameEntry.Clear();
        nameEntry.SendKeys(name);
        _session.HideKeyboardIfShown();
        return this;
    }

    public IReadOnlyList<string> SourceNames => _session.VisibleTexts(SourceName);

    // --- 3. Credit cards ---

    /// <summary>
    /// Fills the card form with its name, limit and bill due day, then taps "Adicionar cartão de crédito". The bill is
    /// paid by the account already picked (the first account added).
    /// </summary>
    public SetupPageObject AddCard(string name, decimal limit, int billDueDay)
    {
        var count = CardNames.Count;

        var nameEntry = _session.ScrollTo("SetupCardNameEntry");
        nameEntry.Clear();
        nameEntry.SendKeys(name);
        _session.HideKeyboardIfShown();

        TypeAmount(_session.ScrollTo("SetupCardLimitEntry"), AppSession.AutomationId("SetupCardLimitEntry"), limit);
        SelectDay("SetupCardBillDueDayPicker", billDueDay);

        _session.ScrollToAndTap("SetupAddCardButton");
        _session.WaitFor(() => CardNames.Count == count + 1, $"\"{name}\" to be added");
        return this;
    }

    public IReadOnlyList<string> CardNames => ScrolledTexts("SetupCardNameLabel", CardName);

    /// <summary>
    /// "Vence dia 10 · Débito em Nubank" for each card added.
    /// </summary>
    public IReadOnlyList<string> CardDetails => ScrolledTexts("SetupCardDetailLabel", CardDetail);

    /// <summary>
    /// Adds a purchase in installments to the card picked (the first one added): <paramref name="current"/> of
    /// <paramref name="total"/> falls this month.
    /// </summary>
    public SetupPageObject AddInstallmentPurchase(string description, decimal installmentAmount, int current, int total)
    {
        var count = PurchaseDescriptions.Count;

        var descriptionEntry = _session.ScrollTo("SetupPurchaseDescriptionEntry");
        descriptionEntry.Clear();
        descriptionEntry.SendKeys(description);
        _session.HideKeyboardIfShown();

        _session.ScrollToAndTap("SetupPurchaseInstallmentsButton");
        TypeNumber("SetupPurchaseCurrentInstallmentEntry", current);
        TypeNumber("SetupPurchaseTotalInstallmentsEntry", total);
        TypeAmount(_session.ScrollTo("SetupPurchaseAmountEntry"), AppSession.AutomationId("SetupPurchaseAmountEntry"), installmentAmount);

        _session.ScrollToAndTap("SetupAddPurchaseButton");
        _session.WaitFor(() => PurchaseDescriptions.Count == count + 1, $"\"{description}\" to be added");
        return this;
    }

    public IReadOnlyList<string> PurchaseDescriptions => ScrolledTexts("SetupPurchaseDescriptionLabel", PurchaseDescription);

    // --- 4. Income ---

    public SetupPageObject EnterSalary(decimal amount)
    {
        TypeAmount(_session.ScrollTo(SalaryEntryId), AppSession.AutomationId(SalaryEntryId), amount);
        return this;
    }

    public SetupPageObject SelectSalaryDay(int day) => SelectDay(SalaryDayPickerId, day);

    /// <summary>
    /// Turns on "Recebo o salário em duas partes" and fills the advance ("vale"), which keeps its default day.
    /// </summary>
    public SetupPageObject SplitSalary(decimal advance)
    {
        _session.ScrollToAndTap("SetupSalarySplitSwitch");
        TypeAmount(_session.ScrollTo("SetupAdvanceEntry"), AppSession.AutomationId("SetupAdvanceEntry"), advance);
        return this;
    }

    /// <summary>
    /// "Total no mês": the payment plus the advance.
    /// </summary>
    public string MonthlySalaryText => _session.ScrollTo("SetupMonthlySalaryLabel").Text;

    // --- 5. House expenses and 6. other expenses ---

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

    /// <summary>
    /// Which of "Valor estimado" and "Pagamento parcial" the selected expense offers. The whole row is scrolled above
    /// the footer first, so an option missing from the screen is one the row doesn't have.
    /// </summary>
    public (bool Estimated, bool PartialPayment) ExpenseOptions(string description)
    {
        _session.HideKeyboardIfShown();
        var row = By.XPath($"//*[@resource-id='{E2ESettings.AppPackage}:id/SetupExpenseRow'][.//*[@text='{description}']]");
        _session.ScrollDownUntil(() => IsWhollyOnScreen(row), $"the whole row of \"{description}\" to be on screen");
        return (_session.IsVisible(ExpenseRowElement(description, "SetupExpenseEstimatedOption")),
                _session.IsVisible(ExpenseRowElement(description, "SetupExpensePartialPaymentOption")));
    }

    /// <summary>
    /// Picks how the ticked car insurance is paid: once a year or in <paramref name="installments"/> a year, from
    /// <paramref name="firstMonth"/>.
    /// </summary>
    public SetupPageObject PayCarInsuranceYearly(int installments, int firstMonth)
    {
        _session.HideKeyboardIfShown();
        SelectOption("SetupCarInsurancePaymentPicker", AppLabels.YearlyPayment(installments));
        SelectOption("SetupCarInsuranceMonthPicker", AppLabels.MonthName(firstMonth));
        return this;
    }

    private bool IsOptionChecked(string description, string checkBoxId) =>
        _session.WaitForVisible(ExpenseRowElement(description, checkBoxId)).GetAttribute("checked") == "true";

    /// <summary>
    /// The account or credit card the selected expense's "Pago com" shows.
    /// </summary>
    public string ExpenseSource(string description)
    {
        ShowOption(description, "SetupExpenseSourcePicker");
        return _session.WaitForVisible(ExpenseRowElement(description, "SetupExpenseSourcePicker")).Text;
    }

    /// <summary>
    /// Picks the account or credit card that pays the selected expense ("Pago com" in its row).
    /// </summary>
    public SetupPageObject SelectExpenseSource(string description, string sourceName)
    {
        ShowOption(description, "SetupExpenseSourcePicker");
        var picker = ExpenseRowElement(description, "SetupExpenseSourcePicker");
        _session.Tap(picker);
        _session.ScrollDialogListToAndTap(sourceName);
        _session.WaitForText(picker, sourceName);
        return this;
    }

    /// <summary>
    /// Answers "Você tem filhos?" with "Sim", which shows the school expenses.
    /// </summary>
    public SetupPageObject SelectHasChildren()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap("SetupHasChildrenButton");
        // The revealed expenses open below the fold.
        ScrollToText("Mensalidade escolar");
        return this;
    }

    /// <summary>
    /// Answers "Você tem carro?" with "Sim", which shows the car expenses.
    /// </summary>
    public SetupPageObject SelectHasCar()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap("SetupHasCarButton");
        ScrollToText("Combustível");
        return this;
    }

    /// <summary>
    /// Answers "Você tem casa própria?" with "Sim", which shows the home form.
    /// </summary>
    public SetupPageObject SelectHasOwnHome()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap("SetupHasOwnHomeButton");
        _session.ScrollTo("SetupHomeNameEntry");
        return this;
    }

    /// <summary>
    /// Adds a home with its IPTU (the amount of each installment), which keeps its default first month (February).
    /// </summary>
    public SetupPageObject AddHome(string name, decimal iptu, int iptuInstallments)
    {
        var count = ScrolledTexts("SetupHomeNameLabel", AppSession.AutomationId("SetupHomeNameLabel")).Count;
        TypeText("SetupHomeNameEntry", name);
        TypeAmount(_session.ScrollTo("SetupHomeIptuEntry"), AppSession.AutomationId("SetupHomeIptuEntry"), iptu);
        SelectOption("SetupHomeIptuInstallmentsPicker", AppLabels.AnnualInstallments(iptuInstallments));
        _session.ScrollToAndTap("SetupAddHomeButton");
        _session.WaitFor(() => ScrolledTexts("SetupHomeNameLabel", AppSession.AutomationId("SetupHomeNameLabel")).Count == count + 1,
            $"\"{name}\" to be added");
        return this;
    }

    /// <summary>
    /// Adds a car with its IPVA (the amount of each installment, from January) and its licensing fee (in July), the
    /// default months.
    /// </summary>
    public SetupPageObject AddCar(string name, decimal ipva, int ipvaInstallments, decimal licensing)
    {
        var count = ScrolledTexts("SetupCarNameLabel", AppSession.AutomationId("SetupCarNameLabel")).Count;
        TypeText("SetupCarNameEntry", name);
        TypeAmount(_session.ScrollTo("SetupCarIpvaEntry"), AppSession.AutomationId("SetupCarIpvaEntry"), ipva);
        SelectOption("SetupCarIpvaInstallmentsPicker", AppLabels.AnnualInstallments(ipvaInstallments));
        TypeAmount(_session.ScrollTo("SetupCarLicensingEntry"), AppSession.AutomationId("SetupCarLicensingEntry"), licensing);
        _session.ScrollToAndTap("SetupAddCarButton");
        _session.WaitFor(() => ScrolledTexts("SetupCarNameLabel", AppSession.AutomationId("SetupCarNameLabel")).Count == count + 1,
            $"\"{name}\" to be added");
        return this;
    }

    /// <summary>
    /// "IPTU todo ano, fevereiro a novembro (10x)" for each home added.
    /// </summary>
    public IReadOnlyList<string> HomeDetails => ScrolledTexts("SetupHomeDetailLabel", AppSession.AutomationId("SetupHomeDetailLabel"));

    public IReadOnlyList<string> CarDetails => ScrolledTexts("SetupCarDetailLabel", AppSession.AutomationId("SetupCarDetailLabel"));

    private void TypeText(string automationId, string text)
    {
        var entry = _session.ScrollTo(automationId);
        entry.Clear();
        entry.SendKeys(text);
        _session.HideKeyboardIfShown();
    }

    private void SelectOption(string pickerId, string label)
    {
        _session.ScrollToAndTap(pickerId);
        _session.ScrollDialogListToAndTap(label);
        _session.WaitForText(AppSession.AutomationId(pickerId), label);
    }

    // --- Skipping ---

    /// <summary>
    /// Taps "Pular esta etapa", which discards what was filled in on the step and moves on.
    /// </summary>
    public SetupPageObject SkipStep()
    {
        var step = CurrentStep;
        _session.HideKeyboardIfShown();
        _session.Tap(AppSession.AutomationId("SetupSkipStepButton"));
        return WaitForStep(step + 1);
    }

    /// <summary>
    /// "Tudo é opcional...", shown on every step but the summary.
    /// </summary>
    public bool IsOptionalHintVisible => _session.IsVisible(AppSession.AutomationId("SetupOptionalHint"));

    /// <summary>
    /// "Etapas puladas: Contas e vales, Renda" on the summary, see <see cref="AppLabels.SetupSkippedSteps"/>.
    /// </summary>
    public string SkippedStepsText => _session.ScrollTo("SetupSkippedStepsLabel").Text;

    // --- 7. Summary ---

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

    // The card and purchase lists sit above their forms, so they scroll out of sight while a form is filled.
    private IReadOnlyList<string> ScrolledTexts(string automationId, By locator)
    {
        _session.TryScrollTo(automationId);
        return _session.VisibleTexts(locator);
    }

    private void TypeNumber(string automationId, int value)
    {
        var entry = _session.ScrollTo(automationId);
        entry.Clear();
        entry.SendKeys(value.ToString(CultureInfo.InvariantCulture));
        _session.WaitForText(AppSession.AutomationId(automationId), value.ToString(CultureInfo.InvariantCulture));
        _session.HideKeyboardIfShown();
    }

    private SetupPageObject SelectDay(string pickerId, int day)
    {
        _session.ScrollToAndTap(pickerId);
        _session.ScrollDialogListToAndTap(AppLabels.DueDay(day));
        _session.WaitForText(AppSession.AutomationId(pickerId), AppLabels.DueDay(day));
        return this;
    }

    // A partly hidden element reports its bounds clipped to the scroll view, so its bottom edge then sits on the
    // scroll view's: only one ending above it is shown whole.
    private bool IsWhollyOnScreen(By locator)
    {
        var scrollView = _session.Driver.FindElements(By.XPath("//android.widget.ScrollView[@scrollable='true']")).First(e => e.Displayed);
        var top = scrollView.Location.Y;
        var bottom = top + scrollView.Size.Height;
        return _session.Driver.FindElements(locator).Any(e => e.Displayed && e.Location.Y > top && e.Location.Y + e.Size.Height < bottom);
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
