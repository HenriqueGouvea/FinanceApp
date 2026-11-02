using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// "Configurações": the user's name and photo, the app lock and the setup wizard.
/// </summary>
public class SettingsPageObject
{
    private static readonly By ProfileRow = AppSession.AutomationId("SettingsProfileRow");
    private static readonly By ProfileName = AppSession.AutomationId("SettingsProfileNameLabel");
    private static readonly By SecurityRow = AppSession.AutomationId("SettingsSecurityRow");
    private static readonly By ProfileNameEntry = AppSession.AutomationId("ProfileNameEntry");
    private static readonly By SaveProfileButton = AppSession.AutomationId("SaveProfileButton");
    private static readonly By FlyoutButton =
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.ImageButton\").instance(0)");

    private readonly AppSession _session;

    public SettingsPageObject(AppSession session)
    {
        _session = session;
    }

    public SettingsPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(ProfileRow);
        return this;
    }

    /// <summary>
    /// The section headers, top to bottom.
    /// </summary>
    public IReadOnlyList<string> SectionHeaders =>
        new[] { "SettingsUserHeader", "SettingsSecurityHeader", "SettingsGuideHeader" }
            .Select(id => _session.ScrollTo(id).Text)
            .ToList();

    public string ProfileNameText => _session.WaitForVisible(ProfileName).Text;

    /// <summary>
    /// Opens "Nome e foto", types the name and saves it.
    /// </summary>
    public SettingsPageObject EditName(string name)
    {
        _session.Tap(ProfileRow);
        var entry = _session.WaitForVisible(ProfileNameEntry);
        entry.Clear();
        entry.SendKeys(name);
        _session.CloseKeyboardWithDone();
        _session.Tap(SaveProfileButton);
        _session.WaitForGone(SaveProfileButton);
        _session.WaitForText(ProfileName, name);
        return this;
    }

    public SecurityPageObject OpenSecurity()
    {
        _session.Tap(SecurityRow);
        return new SecurityPageObject(_session).WaitUntilLoaded();
    }

    public SetupPageObject RedoSetup()
    {
        _session.ScrollToAndTap("SettingsRedoSetupRow");
        return new SetupPageObject(_session).WaitUntilLoaded();
    }

    public MainPageObject GoToHome()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.HomeMenuItem));
        return new MainPageObject(_session).WaitUntilLoaded();
    }
}
