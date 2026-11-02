using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// The full-screen PIN keypad: the lock screen, and the create/change/disable PIN flows from "Segurança".
/// </summary>
public class PinPageObject
{
    private static readonly By TitleLabel = AppSession.AutomationId("PinTitleLabel");
    private static readonly By DotsLabel = AppSession.AutomationId("PinDotsLabel");
    private static readonly By ErrorLabel = AppSession.AutomationId("PinErrorLabel");
    private static readonly By CancelButton = AppSession.AutomationId("PinCancelButton");
    private static readonly By ForgotPinButton = AppSession.AutomationId("ForgotPinButton");
    private static readonly By DeleteKey = AppSession.AutomationId("PinDeleteKey");

    private readonly AppSession _session;

    public PinPageObject(AppSession session)
    {
        _session = session;
    }

    /// <summary>
    /// The lock screen shows up once the app has loaded after a cold start, so allow for the startup time.
    /// </summary>
    public PinPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(TitleLabel, TimeSpan.FromSeconds(60));
        return this;
    }

    public bool IsVisible => _session.IsVisible(TitleLabel);

    public string TitleText => _session.WaitForVisible(TitleLabel).Text;

    public string DotsText => _session.WaitForVisible(DotsLabel).Text;

    public bool IsCancelVisible => _session.IsVisible(CancelButton);

    public bool IsForgotPinVisible => _session.IsVisible(ForgotPinButton);

    public PinPageObject WaitForTitle(string title)
    {
        _session.WaitForText(TitleLabel, title);
        return this;
    }

    public PinPageObject WaitForError(string error)
    {
        _session.WaitForText(ErrorLabel, error);
        return this;
    }

    /// <summary>
    /// Types the digits on the keypad. The fourth digit submits the step.
    /// </summary>
    public PinPageObject Type(string digits)
    {
        foreach (var digit in digits)
            _session.Tap(AppSession.AutomationId($"PinKey{digit}"));
        return this;
    }

    public PinPageObject DeleteDigit()
    {
        _session.Tap(DeleteKey);
        return this;
    }

    /// <summary>
    /// Types a PIN that closes the keypad (the right PIN to unlock or disable, or the confirmation of a new one).
    /// </summary>
    public void TypeAndWaitForClose(string pin)
    {
        Type(pin);
        WaitUntilClosed();
    }

    public void WaitUntilClosed() => _session.WaitForGone(TitleLabel);

    public void Cancel()
    {
        _session.Tap(CancelButton);
        WaitUntilClosed();
    }

    /// <summary>
    /// Opens the "Esqueci o PIN" explanation and returns whether it says the PIN can't be recovered, then closes it.
    /// </summary>
    public bool ForgotPinExplainsNoRecovery()
    {
        _session.Tap(ForgotPinButton);
        var explains = _session.WaitForVisible(AppSession.TextContaining(AppLabels.PinNotRecoverable)).Displayed;
        _session.Tap(AppSession.DialogOption(AppLabels.Understood));
        _session.WaitForGone(AppSession.TextContaining(AppLabels.PinNotRecoverable));
        return explains;
    }
}
