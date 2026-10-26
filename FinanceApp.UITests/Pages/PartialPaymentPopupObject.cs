using System.Globalization;
using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// The "Pagar parcial" / "Receber parcial" popup opened from the entry actions menu.
/// </summary>
public class PartialPaymentPopupObject
{
    private static readonly By Title = AppSession.AutomationId("PartialPaymentTitleLabel");
    private static readonly By Description = AppSession.AutomationId("PartialPaymentDescriptionLabel");
    private static readonly By Progress = AppSession.AutomationId("PartialPaymentProgressLabel");
    private static readonly By NewTotal = AppSession.AutomationId("PartialPaymentNewTotalLabel");
    private static readonly By AmountEntry = AppSession.AutomationId("PartialPaymentAmountEntry");
    private const string SaveButtonId = "SavePartialPaymentButton";
    private const string CancelButtonId = "CancelPartialPaymentButton";

    private readonly AppSession _session;

    public PartialPaymentPopupObject(AppSession session)
    {
        _session = session;
    }

    public PartialPaymentPopupObject WaitUntilOpen()
    {
        _session.WaitForVisible(AmountEntry);
        return this;
    }

    public string TitleText => _session.WaitForVisible(Title).Text;
    public string DescriptionText => _session.WaitForVisible(Description).Text;
    public string ProgressText => _session.WaitForVisible(Progress).Text;
    public string NewTotalText => _session.WaitForVisible(NewTotal).Text;
    public bool IsSaveEnabled => _session.WaitForVisible(AppSession.AutomationId(SaveButtonId)).Enabled;

    /// <summary>
    /// Masked as currency: typed digits are read as cents.
    /// </summary>
    public PartialPaymentPopupObject EnterAmount(decimal amount)
    {
        var entry = _session.WaitForVisible(AmountEntry);
        entry.Clear();
        entry.SendKeys(decimal.ToInt64(amount * 100).ToString(CultureInfo.InvariantCulture));
        _session.WaitForText(AmountEntry, AppFormat.Money(amount));
        _session.CloseKeyboardWithDone();
        return this;
    }

    public void Save()
    {
        _session.CloseKeyboardWithDone();
        _session.ScrollToAndTap(SaveButtonId);
        _session.WaitForGone(AppSession.AutomationId(SaveButtonId));
    }

    public void Cancel()
    {
        _session.CloseKeyboardWithDone();
        _session.ScrollToAndTap(CancelButtonId);
        _session.WaitForGone(AppSession.AutomationId(CancelButtonId));
    }
}
