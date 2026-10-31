using System.Drawing;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Support.UI;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// One Appium session shared by the whole run (see <see cref="AppiumServerFixture.StartApp"/>). App data is wiped
/// before every test, so each one starts with a fresh database.
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
        // The device setup and the UiAutomator2 server install only need to happen for the first session of a run.
        options.AddAdditionalAppiumOption("appium:skipDeviceInitialization", server.IsAppInstalled);
        options.AddAdditionalAppiumOption("appium:skipServerInstallation", server.IsAppInstalled);
        options.AddAdditionalAppiumOption("appium:disableWindowAnimation", true);
        // UiAutomator2 waits up to 10 s for the UI to go idle before every command. Tests always wait for the
        // state they expect, so that idle wait only adds latency.
        options.AddAdditionalAppiumOption("appium:settings[waitForIdleTimeout]", 0);

        Driver = new AndroidDriver(server.ServerUri, options, TimeSpan.FromMinutes(3));
        server.IsAppInstalled = true;
        Database = new DeviceDatabase();
    }

    /// <summary>
    /// Wipes the app's data (which also stops it) and launches it again, as a new session would but without
    /// the cost of starting one.
    /// </summary>
    public void RestartWithEmptyData()
    {
        Driver.ExecuteScript("mobile: clearApp", new Dictionary<string, object> { ["appId"] = E2ESettings.AppPackage });
        Driver.ActivateApp(E2ESettings.AppPackage);
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
        ScrollTo(automationId);
        TapWhenSettled(AutomationId(automationId));
    }

    /// <summary>
    /// Taps once the element stops moving, e.g. while a popup re-centers after the keyboard hides,
    /// so the tap can't land where the element used to be.
    /// </summary>
    public void TapWhenSettled(By locator)
    {
        var last = Rectangle.Empty;
        var element = WaitUntil(d =>
        {
            var current = d.FindElements(locator).FirstOrDefault(e => e.Displayed);
            if (current is null)
                return null;

            var bounds = new Rectangle(current.Location, current.Size);
            var settled = bounds == last;
            last = bounds;
            return settled ? current : null;
        }, $"{locator} to stop moving", null)!;
        element.Click();
    }

    public IWebElement ScrollTo(string automationId)
    {
        TryScrollTo(automationId);
        return WaitForVisible(AutomationId(automationId));
    }

    /// <summary>
    /// Scrolls until the element is on screen and returns it, or null without waiting when it doesn't exist.
    /// The keyboard is hidden first: a scroll swipe over it would glide-type into the focused entry.
    /// </summary>
    public IWebElement? TryScrollTo(string automationId)
    {
        HideKeyboardIfShown();

        var selector = $"new UiSelector().resourceId(\"{E2ESettings.AppPackage}:id/{automationId}\")";
        return Driver.FindElements(MobileBy.AndroidUIAutomator(
                $"new UiScrollable(new UiSelector().scrollable(true).packageName(\"{E2ESettings.AppPackage}\")).scrollIntoView({selector})"))
            .FirstOrDefault(e => e.Displayed);
    }

    /// <summary>
    /// Scrolls the open picker dialog's list until the option with that exact text is on screen, for lists
    /// longer than the dialog (e.g. the 31 due days), then taps it once the list stops gliding: a tap on a
    /// moving list only stops it.
    /// </summary>
    public void ScrollDialogListToAndTap(string text)
    {
        WaitForVisible(MobileBy.AndroidUIAutomator(
            $"new UiScrollable(new UiSelector().className(\"android.widget.ListView\")).scrollIntoView(new UiSelector().text(\"{text}\"))"));
        TapWhenSettled(Text(text));
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
    /// Scrolls the page content up until the element ends above <paramref name="limitY"/> (e.g. the top of floating buttons).
    /// </summary>
    public void ScrollAbove(Func<IWebElement> element, int limitY)
    {
        bool IsClear()
        {
            var e = element();
            return e.Location.Y + e.Size.Height <= limitY;
        }

        if (IsClear())
            return;

        var size = Driver.Manage().Window.Size;
        Driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
        {
            ["left"] = size.Width / 10,
            ["top"] = size.Height / 4,
            ["width"] = size.Width * 8 / 10,
            ["height"] = size.Height / 2,
            ["direction"] = "down",
            ["percent"] = 1.0
        });
        WaitFor(IsClear, "the element to scroll above the floating buttons");
    }

    /// <summary>
    /// Swipes horizontally across the lower middle of the screen, below the month summary (which takes
    /// sideways swipes for its own pages). "left" moves the finger to the left.
    /// </summary>
    public void SwipeHorizontally(string direction)
    {
        var size = Driver.Manage().Window.Size;
        Swipe(new Rectangle(size.Width / 10, size.Height / 2, size.Width * 8 / 10, size.Height / 5), direction);
    }

    /// <summary>
    /// Swipes horizontally across the middle of the element. "left" moves the finger to the left.
    /// The swipe stays clear of the screen edges, where Android's gesture navigation would take it as "back".
    /// </summary>
    public void SwipeOn(IWebElement element, string direction) =>
        Swipe(new Rectangle(
            element.Location.X + element.Size.Width / 5, element.Location.Y,
            element.Size.Width * 3 / 5, element.Size.Height), direction);

    private void Swipe(Rectangle area, string direction) =>
        Driver.ExecuteScript("mobile: swipeGesture", new Dictionary<string, object>
        {
            ["left"] = area.X,
            ["top"] = area.Y,
            ["width"] = area.Width,
            ["height"] = area.Height,
            ["direction"] = direction,
            ["percent"] = 0.75
        });

    /// <summary>
    /// Decodes the element's on-screen pixels, for asserting on rendered colors.
    /// </summary>
    public static PngImage Capture(IWebElement element) =>
        PngImage.Decode(((ITakesScreenshot)element).GetScreenshot().AsByteArray);

    public void PressBack() => Driver.Navigate().Back();

    /// <summary>
    /// Closes the keyboard through its "Done" action. Unlike <see cref="HideKeyboardIfShown"/>, it can never fall
    /// back to pressing Back, which would dismiss a popup without saving.
    /// </summary>
    public void CloseKeyboardWithDone()
    {
        if (!Driver.IsKeyboardShown())
            return;

        Driver.ExecuteScript("mobile: performEditorAction", new Dictionary<string, object> { ["action"] = "done" });
        WaitFor(() => !Driver.IsKeyboardShown(), "the keyboard to close");
    }

    public void HideKeyboardIfShown()
    {
        if (!Driver.IsKeyboardShown())
            return;

        Driver.HideKeyboard();
        WaitFor(() => !Driver.IsKeyboardShown(), "the keyboard to hide");
    }

    public void Dispose() => Driver.Quit();

    private static Point Center(Point location, Size size) =>
        new(location.X + size.Width / 2, location.Y + size.Height / 2);

    private TResult WaitUntil<TResult>(Func<IWebDriver, TResult> condition, string description, TimeSpan? timeout)
    {
        var wait = new WebDriverWait(Driver, timeout ?? E2ESettings.DefaultTimeout)
        {
            PollingInterval = E2ESettings.PollingInterval
        };
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
