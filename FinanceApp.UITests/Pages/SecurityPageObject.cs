using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

public class SecurityPageObject
{
    private static readonly By NoAccountNotice = AppSession.AutomationId("NoAccountNotice");
    private static readonly By PinLockRow = AppSession.AutomationId("PinLockRow");
    private static readonly By PinLockSwitch = AppSession.AutomationId("PinLockSwitch");
    private static readonly By ChangePinRow = AppSession.AutomationId("ChangePinRow");
    private static readonly By BiometricRow = AppSession.AutomationId("BiometricRow");

    private readonly AppSession _session;

    public SecurityPageObject(AppSession session)
    {
        _session = session;
    }

    public SecurityPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(PinLockRow);
        return this;
    }

    public bool IsNoAccountNoticeVisible => _session.IsVisible(NoAccountNotice);

    public bool IsPinLockOn => _session.WaitForVisible(PinLockSwitch).GetAttribute("checked") == "true";

    public bool IsChangePinVisible => _session.IsVisible(ChangePinRow);

    public bool IsBiometricOptionVisible => _session.IsVisible(BiometricRow);

    /// <summary>
    /// Waits for the "Bloquear com PIN" switch to settle, as the page reloads after the keypad closes.
    /// </summary>
    public SecurityPageObject WaitForPinLock(bool on)
    {
        _session.WaitFor(() => IsPinLockOn == on, $"the PIN lock switch to be {(on ? "on" : "off")}");
        return this;
    }

    /// <summary>
    /// Taps "Bloquear com PIN": creates a PIN when it's off, asks for the current one to turn it off when it's on.
    /// </summary>
    public PinPageObject TapPinLock()
    {
        _session.Tap(PinLockRow);
        return new PinPageObject(_session).WaitUntilLoaded();
    }

    public PinPageObject TapChangePin()
    {
        _session.Tap(ChangePinRow);
        return new PinPageObject(_session).WaitUntilLoaded();
    }

    public SecurityPageObject EnablePin(string pin)
    {
        TapPinLock()
            .Type(pin)
            .WaitForTitle(AppLabels.ConfirmPin)
            .TypeAndWaitForClose(pin);
        return WaitForPinLock(true);
    }

    /// <summary>
    /// The page is pushed from the settings, so Back returns there before the menu leads to "Início".
    /// </summary>
    public MainPageObject GoToEntries()
    {
        _session.PressBack();
        return new SettingsPageObject(_session).WaitUntilLoaded().GoToHome();
    }
}
