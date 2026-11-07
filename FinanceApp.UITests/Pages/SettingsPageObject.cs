using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// "Configurações": the user's name and photo, the app lock, the setup wizard and the user's data (export, import, wipe).
/// </summary>
public class SettingsPageObject
{
    private static readonly By ProfileRow = AppSession.AutomationId("SettingsProfileRow");
    private static readonly By ProfileName = AppSession.AutomationId("SettingsProfileNameLabel");
    private static readonly By SecurityRow = AppSession.AutomationId("SettingsSecurityRow");
    private static readonly By ProfileNameEntry = AppSession.AutomationId("ProfileNameEntry");
    private static readonly By SaveProfileButton = AppSession.AutomationId("SaveProfileButton");
    // The share sheet belongs to the system ("android" up to Android 13, "com.android.intentresolver" from 14).
    private static readonly By ShareSheetFileName =
        MobileBy.AndroidUIAutomator("new UiSelector().resourceIdMatches(\".*:id/content_preview_filename\")");
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
        new[] { "SettingsUserHeader", "SettingsSecurityHeader", "SettingsGuideHeader", "SettingsDataHeader" }
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

    /// <summary>
    /// Taps "Exportar dados" and waits for Android's share sheet, which names the first shared file (the backup).
    /// Returns that file name; close the sheet with <see cref="CloseShareSheet"/>.
    /// </summary>
    public string ExportData()
    {
        _session.ScrollToAndTap("SettingsExportRow");
        return _session.WaitForVisible(ShareSheetFileName).Text;
    }

    public SettingsPageObject CloseShareSheet()
    {
        _session.PressBack();
        _session.WaitForGone(ShareSheetFileName);
        _session.WaitForVisible(ProfileRow);
        return this;
    }

    /// <summary>
    /// Taps "Importar backup", picks the file from Downloads, confirms and lands on "Início".
    /// </summary>
    public MainPageObject ImportBackup(string fileName)
    {
        _session.ScrollToAndTap("SettingsImportRow");
        new SystemFilePickerObject(_session).Pick(fileName);
        _session.Tap(AppSession.DialogOption(AppLabels.ImportBackupConfirm));
        return new MainPageObject(_session).WaitUntilLoaded();
    }

    /// <summary>
    /// Taps "Apagar todos os dados" and confirms; the app starts over with the setup wizard.
    /// </summary>
    public SetupPageObject DeleteAllData()
    {
        _session.ScrollToAndTap("SettingsDeleteAllRow");
        _session.Tap(AppSession.DialogOption(AppLabels.DeleteAllDataConfirm));
        return new SetupPageObject(_session).WaitUntilLoaded();
    }

    public MainPageObject GoToHome()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.HomeMenuItem));
        return new MainPageObject(_session).WaitUntilLoaded();
    }
}
