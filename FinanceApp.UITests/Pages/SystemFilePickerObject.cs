using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// Android's file picker (DocumentsUI), opened by "Importar backup". Its texts follow the device language, so files
/// are found by name and the menu by its content description.
/// </summary>
public class SystemFilePickerObject
{
    private static readonly By ShowRoots = MobileBy.AccessibilityId("Show roots");
    private static readonly By DownloadsRoot = MobileBy.AndroidUIAutomator("new UiSelector().textMatches(\"(?i)^downloads?$\")");

    private readonly AppSession _session;

    public SystemFilePickerObject(AppSession session)
    {
        _session = session;
    }

    /// <summary>
    /// Picks a file added with <see cref="DeviceFiles.AddDownload"/>: from "Recent" when listed there, otherwise from Downloads.
    /// </summary>
    public void Pick(string fileName)
    {
        var file = AppSession.Text(fileName);
        _session.WaitFor(() => _session.IsVisible(file) || _session.IsVisible(ShowRoots), "the file picker to open");

        if (!_session.IsVisible(file))
        {
            _session.Tap(ShowRoots);
            _session.Tap(DownloadsRoot);
        }

        _session.Tap(file);
    }
}
