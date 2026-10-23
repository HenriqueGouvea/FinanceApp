using System.Drawing;
using System.Text.RegularExpressions;
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

    public static By AutomationIdWithText(string automationId, string text) =>
        MobileBy.AndroidUIAutomator(
            $"new UiSelector().resourceId(\"{E2ESettings.AppPackage}:id/{automationId}\").text(\"{text}\")");

    public static By Text(string text) =>
        MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{text}\")");

    /// <summary>
    /// Native dialog buttons may be rendered in upper case, so dialog options are matched case-insensitively.
    /// </summary>
    public static By DialogOption(string text) =>
        MobileBy.AndroidUIAutomator($"new UiSelector().textMatches(\"(?i)^{Regex.Escape(text)}$\")");

    /// <summary>
    /// Returns the first on-screen match. The month CarouselView keeps neighbour months in the
    /// view tree, so the same AutomationId can exist more than once.
    /// </summary>
    public IWebElement WaitForVisible(By locator, TimeSpan? timeout = null) =>
        WaitUntil(d => d.FindElements(locator).FirstOrDefault(e => e.Displayed), $"{locator} to be visible", timeout)!;

    public void WaitForGone(By locator, TimeSpan? timeout = null) =>
        WaitUntil(d => !d.FindElements(locator).Any(e => e.Displayed), $"{locator} to disappear", timeout);

    public void WaitForText(By locator, string expectedText, TimeSpan? timeout = null) =>
        WaitUntil(d => d.FindElements(locator).Any(e => e.Displayed && e.Text == expectedText),
            $"{locator} to show \"{expectedText}\"", timeout);

    public void WaitFor(Func<bool> condition, string description, TimeSpan? timeout = null) =>
        WaitUntil(_ => condition(), description, timeout);

    public bool IsVisible(By locator) => Driver.FindElements(locator).Any(e => e.Displayed);

    public IReadOnlyList<string> VisibleTexts(By locator) =>
        Driver.FindElements(locator).Where(e => e.Displayed).Select(e => e.Text).ToList();

    public void Tap(By locator) => WaitForVisible(locator).Click();

    /// <summary>
    /// Scrolls the nearest scrollable container until the element is on screen, then taps it.
    /// Needed for popups whose buttons sit below the fold on small screens. Scoped to the app so system
    /// overlays (e.g. a heads-up notification) are never scrolled instead.
    /// </summary>
    public void ScrollToAndTap(string automationId)
    {
        var selector = $"new UiSelector().resourceId(\"{E2ESettings.AppPackage}:id/{automationId}\")";
        Driver.FindElements(MobileBy.AndroidUIAutomator(
            $"new UiScrollable(new UiSelector().scrollable(true).packageName(\"{E2ESettings.AppPackage}\")).scrollIntoView({selector})"));
        Tap(AutomationId(automationId));
    }

    public void LongPress(IWebElement element, int durationMs = 1500)
    {
        var center = Center(element.Location, element.Size);
        Driver.ExecuteScript("mobile: longClickGesture", new Dictionary<string, object>
        {
            ["x"] = center.X,
            ["y"] = center.Y,
            ["duration"] = durationMs
        });
    }

    /// <summary>
    /// Swipes horizontally across the middle of the screen. "left" moves the finger to the left.
    /// </summary>
    public void SwipeHorizontally(string direction)
    {
        var size = Driver.Manage().Window.Size;
        Driver.ExecuteScript("mobile: swipeGesture", new Dictionary<string, object>
        {
            ["left"] = size.Width / 10,
            ["top"] = size.Height / 3,
            ["width"] = size.Width * 8 / 10,
            ["height"] = size.Height / 5,
            ["direction"] = direction,
            ["percent"] = 0.75
        });
    }

    /// <summary>
    /// Decodes the element's on-screen pixels, for asserting on rendered colors.
    /// </summary>
    public static PngImage Capture(IWebElement element) =>
        PngImage.Decode(((ITakesScreenshot)element).GetScreenshot().AsByteArray);

    public void PressBack() => Driver.Navigate().Back();

    public void HideKeyboardIfShown()
    {
        if (Driver.IsKeyboardShown())
            Driver.HideKeyboard();
    }

    public void Dispose() => Driver.Quit();

    private static Point Center(Point location, Size size) =>
        new(location.X + size.Width / 2, location.Y + size.Height / 2);

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
}
