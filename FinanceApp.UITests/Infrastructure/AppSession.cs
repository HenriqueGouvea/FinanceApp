using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Support.UI;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// One Appium session per test. App data is wiped at session start (noReset=false), so every test starts with a fresh database.
/// </summary>
public sealed class AppSession : IDisposable
{
    public AndroidDriver Driver { get; }
    public DeviceDatabase Database { get; }

    public AppSession(AppiumServerFixture server)
    {
        var apk = new FileInfo(E2ESettings.ApkPath);
        if (!apk.Exists)
            throw new InvalidOperationException(
                $"APK not found at {apk.FullName}. Build it with the command in FinanceApp.UITests/README.md or set E2E_APK_PATH.");

        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = AutomationName.AndroidUIAutomator2,
            DeviceName = E2ESettings.DeviceName,
            App = apk.FullName
        };
        options.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, false);
        options.AddAdditionalAppiumOption("appium:udid", E2ESettings.DeviceName);
        options.AddAdditionalAppiumOption("appium:autoGrantPermissions", true);
        options.AddAdditionalAppiumOption("appium:newCommandTimeout", 120);
        options.AddAdditionalAppiumOption("appium:enforceAppInstall", !server.IsAppInstalled);

        Driver = new AndroidDriver(server.ServerUri, options, TimeSpan.FromMinutes(3));
        server.IsAppInstalled = true;
        Database = new DeviceDatabase();
    }

    public static By AutomationId(string automationId) => MobileBy.Id(automationId);

    public static By Text(string text) =>
        MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{text}\")");

    /// <summary>
    /// Returns the first on-screen match. The month CarouselView keeps neighbour months in the
    /// view tree, so the same AutomationId can exist more than once.
    /// </summary>
    public IWebElement WaitForVisible(By locator, TimeSpan? timeout = null) =>
        WaitUntil(d => d.FindElements(locator).FirstOrDefault(e => e.Displayed), $"{locator} to be visible", timeout)!;

    public void WaitForText(By locator, string expectedText, TimeSpan? timeout = null) =>
        WaitUntil(d => d.FindElements(locator).Any(e => e.Displayed && e.Text == expectedText),
            $"{locator} to show \"{expectedText}\"", timeout);

    public bool IsVisible(By locator) => Driver.FindElements(locator).Any(e => e.Displayed);

    public void HideKeyboardIfShown()
    {
        if (Driver.IsKeyboardShown())
            Driver.HideKeyboard();
    }

    private TResult WaitUntil<TResult>(Func<IWebDriver, TResult> condition, string description, TimeSpan? timeout)
    {
        var wait = new WebDriverWait(Driver, timeout ?? E2ESettings.DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        try
        {
            return wait.Until(condition);
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new WebDriverTimeoutException($"Timed out waiting for {description}. Diagnostics: {SaveDiagnostics()}", ex);
        }
    }

    private string SaveDiagnostics()
    {
        var directory = Directory.CreateDirectory(Path.Combine(E2ESettings.ProjectDirectory, "TestResults", "diagnostics"));
        var baseName = Path.Combine(directory.FullName, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));

        File.WriteAllText($"{baseName}.xml", Driver.PageSource);
        Driver.GetScreenshot().SaveAsFile($"{baseName}.png");
        return $"{baseName}.xml/.png";
    }

    public void Dispose() => Driver.Quit();
}
