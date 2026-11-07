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

    public string ErrorText => _session.Driver.FindElements(ErrorLabel).FirstOrDefault(e => e.Displayed)?.Text ?? string.Empty;

    /// <summary>
    /// The keys are disabled while too many wrong PINs keep the keypad locked (and briefly while a PIN is checked).
    /// </summary>
    public bool IsKeypadEnabled => _session.WaitForVisible(AppSession.AutomationId("PinKey0")).Enabled;

    /// <summary>
    /// Types a wrong PIN and waits until it's rejected: the first digit clears the previous error, and checking the
    /// PIN sets it again. Then the keypad takes digits again, unless the rejection locked it.
    /// </summary>
    public PinPageObject TypeWrongPin(string pin)
    {
        Type(pin);
        _session.WaitFor(
            () => ErrorText.StartsWith(AppLabels.TooManyAttempts) || (ErrorText.Length > 0 && IsKeypadEnabled),
            "the wrong PIN to be rejected");
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
