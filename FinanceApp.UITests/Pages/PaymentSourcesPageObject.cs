using System.Globalization;
using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

public class PaymentSourcesPageObject
{
    private static readonly By AddButton = AppSession.AutomationId("AddPaymentSourceButton");
    private static readonly By EmptyLabel = AppSession.AutomationId("PaymentSourcesEmptyLabel");
    private static readonly By SourceRow = AppSession.AutomationId("PaymentSourceRow");
    private static readonly By SourceName = AppSession.AutomationId("PaymentSourceNameLabel");
    private static readonly By SourceBalance = AppSession.AutomationId("PaymentSourceBalanceLabel");
    private static readonly By DeleteButton = AppSession.AutomationId("DeletePaymentSourceButton");
    private static readonly By GroupHeader = AppSession.AutomationId("PaymentSourceGroupHeader");
    private static readonly By NameEntry = AppSession.AutomationId("PaymentSourceNameEntry");
    private static readonly By BalanceEntry = AppSession.AutomationId("PaymentSourceBalanceEntry");
    private static readonly By BalanceFieldLabel = AppSession.AutomationId("PaymentSourceBalanceFieldLabel");
    private const string SaveButtonId = "SavePaymentSourceButton";
    private static readonly By FlyoutButton =
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.ImageButton\").instance(0)");

    private readonly AppSession _session;

    public PaymentSourcesPageObject(AppSession session)
    {
        _session = session;
    }

    public PaymentSourcesPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(AddButton);
        return this;
    }

    public bool IsEmptyStateVisible => _session.IsVisible(EmptyLabel);

    public IReadOnlyList<string> VisibleNames => _session.VisibleTexts(SourceName);

    public IReadOnlyList<string> VisibleGroupHeaders => _session.VisibleTexts(GroupHeader);

    /// <summary>
    /// Header of the group ("Contas bancárias"/"Cartões de crédito"/"Vales") the source is listed under, i.e. the closest header above it.
    /// </summary>
    public string GroupOf(string name)
    {
        var sourceY = _session.WaitForVisible(NameWithText(name)).Location.Y;
        return _session.Driver.FindElements(GroupHeader)
            .Where(h => h.Displayed && h.Location.Y < sourceY)
            .MaxBy(h => h.Location.Y)?.Text
            ?? throw new InvalidOperationException($"No group header above \"{name}\".");
    }

    /// <summary>
    /// The row's balance line, e.g. "Saldo: R$ 1.000,00" (see <see cref="AppFormat.SourceBalance"/>).
    /// </summary>
    public string BalanceOf(string name) => FindRow(name).FindElement(SourceBalance).Text;

    /// <summary>
    /// Opens the "+" popup, fills it and saves. <paramref name="type"/> is one of the AppFormat source types.
    /// </summary>
    public PaymentSourcesPageObject AddPaymentSource(string name, int type, decimal balance)
    {
        _session.Tap(AddButton);
        FillAndSave(name, type, balance);
        return this;
    }

    /// <summary>
    /// Taps the row to open its popup, replaces every field and saves.
    /// </summary>
    public PaymentSourcesPageObject EditPaymentSource(string name, string newName, int type, decimal balance)
    {
        FindRow(name).FindElement(SourceName).Click();
        _session.WaitForText(NameEntry, name);
        FillAndSave(newName, type, balance);
        return this;
    }

    /// <summary>
    /// Taps the row's "Excluir" button and confirms the native alert.
    /// </summary>
    public PaymentSourcesPageObject DeletePaymentSource(string name)
    {
        FindRow(name).FindElement(DeleteButton).Click();

        _session.Tap(AppSession.DialogOption(AppLabels.Delete));
        _session.WaitForGone(NameWithText(name));
        return this;
    }

    public MainPageObject GoToEntries()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.EntriesMenuItem));
        return new MainPageObject(_session).WaitUntilLoaded();
    }

    private void FillAndSave(string name, int type, decimal balance)
    {
        var entry = _session.WaitForVisible(NameEntry);
        entry.Clear();
        entry.SendKeys(name);
        _session.HideKeyboardIfShown();

        _session.Tap(AppSession.AutomationId(TypeButtonId(type)));
        _session.WaitForText(BalanceFieldLabel, type == AppFormat.CreditCard ? AppLabels.Limit : AppLabels.Balance);

        var balanceEntry = _session.WaitForVisible(BalanceEntry);
        balanceEntry.Clear();
        balanceEntry.SendKeys(decimal.ToInt64(balance * 100).ToString(CultureInfo.InvariantCulture));
        _session.HideKeyboardIfShown();
        _session.WaitForText(BalanceEntry, AppFormat.Money(balance));

        _session.ScrollToAndTap(SaveButtonId);
        _session.WaitForGone(NameEntry);
        _session.WaitForVisible(NameWithText(name));
    }

    private IWebElement FindRow(string name)
    {
        _session.WaitForVisible(NameWithText(name));
        return _session.Driver.FindElements(SourceRow)
            .First(r => r.Displayed && r.FindElements(SourceName).Any(n => n.Text == name));
    }

    private static string TypeButtonId(int type) => type switch
    {
        AppFormat.BankAccount => "PaymentSourceBankAccountTypeButton",
        AppFormat.CreditCard => "PaymentSourceCreditCardTypeButton",
        AppFormat.MealVoucher => "PaymentSourceMealVoucherTypeButton",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown payment source type.")
    };

    private static By NameWithText(string name) =>
        AppSession.AutomationIdWithText("PaymentSourceNameLabel", name);
}
