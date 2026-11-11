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
    private byte[]? _emptyDatabase;

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
    /// the cost of starting one. When <paramref name="seed"/> is given, the app starts with the default categories
    /// plus whatever it writes, so the first-run wizard skips itself and the entries page shows up directly.
    /// </summary>
    public void RestartWithEmptyData(Action<SQLite.SQLiteConnection>? seed = null)
    {
        if (seed is not null)
            _emptyDatabase ??= CaptureEmptyDatabase();

        ClearApp();
        if (seed is not null)
            Database.Push(_emptyDatabase!, seed);
        Driver.ActivateApp(E2ESettings.AppPackage);
    }

    /// <summary>
    /// The database a fresh install creates (tables and default categories), taken once per run as the base of
    /// every seeded test. Seeding before the app starts saves the extra cold start of seeding a running app.
    /// </summary>
    private byte[] CaptureEmptyDatabase()
    {
        ClearApp();
        Driver.ActivateApp(E2ESettings.AppPackage);
        // The wizard only shows once the database is ready.
        WaitForVisible(AutomationId("SetupStepLabel"), TimeSpan.FromSeconds(60));
        Driver.TerminateApp(E2ESettings.AppPackage);
        return Database.Pull();
    }

    private void ClearApp() =>
        Driver.ExecuteScript("mobile: clearApp", new Dictionary<string, object> { ["appId"] = E2ESettings.AppPackage });

    /// <summary>
    /// Stops the app and launches it again keeping its data, i.e. a cold start (where the PIN lock shows up).
    /// </summary>
    public void Relaunch()
    {
        Driver.TerminateApp(E2ESettings.AppPackage);
        Driver.ActivateApp(E2ESettings.AppPackage);
    }

    /// <summary>
    /// Leaves the app (e.g. after Back closed it), then brings it back without restarting it.
    /// </summary>
    public void BringToForeground() => Driver.ActivateApp(E2ESettings.AppPackage);

    public static By AutomationId(string automationId) => MobileBy.Id(automationId);

    public static By TextContaining(string text) =>
        MobileBy.AndroidUIAutomator($"new UiSelector().textContains(\"{text}\")");

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

    /// <summary>
    /// Scrolls the nearest scrollable container until the element is on screen, then taps it.
    /// Needed for popups whose buttons sit below the fold on small screens. Scoped to the app so system
    /// overlays (e.g. a heads-up notification) are never scrolled instead.
    /// </summary>
    public void ScrollToAndTap(string automationId)
    {
        ScrollTo(automationId);
        Tap(AutomationId(automationId));
    }

    /// <summary>
    /// Taps once the element stops moving (a menu sliding in, a list gliding after a scroll, a popup re-centering
    /// after the keyboard hides). UiAutomator2's idle wait is off, so a tap could otherwise land mid-animation.
    /// </summary>
    public void Tap(By locator) =>
        TapWhenSettled(d => d.FindElements(locator).FirstOrDefault(e => e.Displayed), locator.ToString());

    /// <summary>
    /// Like <see cref="Tap(By)"/>, for an element found earlier (e.g. inside a row whose locator no longer matches).
    /// </summary>
    public void Tap(IWebElement element) => TapWhenSettled(_ => element.Displayed ? element : null, "the element");

    private void TapWhenSettled(Func<IWebDriver, IWebElement?> find, string what)
    {
        var last = Rectangle.Empty;
        var element = WaitUntil(d =>
        {
            var current = find(d);
            if (current is null)
                return null;

            var bounds = new Rectangle(current.Location, current.Size);
            var settled = bounds == last;
            last = bounds;
            return settled ? current : null;
        }, $"{what} to stop moving", null)!;
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
    /// longer than the dialog (e.g. the 31 due days), then taps it.
    /// </summary>
    public void ScrollDialogListToAndTap(string text)
    {
        WaitForVisible(MobileBy.AndroidUIAutomator(
            $"new UiScrollable(new UiSelector().className(\"android.widget.ListView\")).scrollIntoView(new UiSelector().text(\"{text}\"))"));
        Tap(Text(text));
    }

    /// <summary>
    /// Taps a point of the screen given as fractions of its width and height, for areas with no element of their own
    /// (e.g. the dimmed page beside the open hamburger menu).
    /// </summary>
    public void TapAt(double xFraction, double yFraction)
    {
        var size = Driver.Manage().Window.Size;
        Driver.ExecuteScript("mobile: clickGesture", new Dictionary<string, object>
        {
            ["x"] = (int)(size.Width * xFraction),
            ["y"] = (int)(size.Height * yFraction)
        });
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
    /// Scrolls the page content up, half a screen at a time, until the condition holds (e.g. an element that just
    /// appeared below the fold is fully on screen).
    /// The default swipe is fast, so the page flings on past it. Pass <paramref name="slowly"/> when the condition
    /// only holds in a narrow window (e.g. a row's bottom edge inside a small scroll view), which a fling skips.
    /// </summary>
    public void ScrollDownUntil(Func<bool> condition, string description, int maxScrolls = 5, bool slowly = false)
    {
        var size = Driver.Manage().Window.Size;
        var gesture = new Dictionary<string, object>
        {
            ["left"] = size.Width / 10,
            ["top"] = size.Height / 4,
            ["width"] = size.Width * 8 / 10,
            ["height"] = size.Height / 2,
            ["direction"] = "down",
            ["percent"] = 0.5
        };
        if (slowly)
            gesture["speed"] = 500;
        for (var i = 0; i < maxScrolls && !condition(); i++)
            Driver.ExecuteScript("mobile: scrollGesture", gesture);
        WaitFor(condition, description);
    }

    /// <summary>
    /// Texts of every element matching the locator in a list longer than the screen (e.g. on CI's 320×640
    /// emulator): collects the displayed ones, scrolling down half a screen at a time until the page can't
    /// scroll further. Leaves the page scrolled to the bottom.
    /// </summary>
    public IReadOnlyList<string> TextsWhileScrollingDown(By locator, int maxScrolls = 10)
    {
        var size = Driver.Manage().Window.Size;
        var texts = VisibleTexts(locator).Distinct().ToList();
        for (var i = 0; i < maxScrolls; i++)
        {
            var canScrollMore = Driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
            {
                ["left"] = size.Width / 10,
                ["top"] = size.Height / 4,
                ["width"] = size.Width * 8 / 10,
                ["height"] = size.Height / 2,
                ["direction"] = "down",
                ["percent"] = 0.5
            });
            texts.AddRange(VisibleTexts(locator).Distinct().Except(texts).ToList());
            if (canScrollMore is false)
                break;
        }

        return texts;
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
    /// The action is resent until the keyboard is gone: one sent while the keyboard is still coming up is ignored.
    /// </summary>
    public void CloseKeyboardWithDone()
    {
        if (!Driver.IsKeyboardShown())
            return;

        var lastSent = DateTime.MinValue;
        WaitFor(() =>
        {
            if (!Driver.IsKeyboardShown())
                return true;

            if (DateTime.UtcNow - lastSent >= E2ESettings.KeyboardDoneRetryInterval)
            {
                Driver.ExecuteScript("mobile: performEditorAction", new Dictionary<string, object> { ["action"] = "done" });
                lastSent = DateTime.UtcNow;
            }
            return false;
        }, "the keyboard to close");
    }

    /// <summary>
    /// For an entry focused on open: the keyboard shows up asynchronously, if at all (e.g. not with a hardware keyboard).
    /// </summary>
    public void CloseKeyboardWithDoneOnceShown()
    {
        try
        {
            WaitFor(() => Driver.IsKeyboardShown(), "the keyboard to show", E2ESettings.KeyboardShowTimeout);
        }
        catch (WebDriverTimeoutException)
        {
            return;
        }

        CloseKeyboardWithDone();
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
            return wait.Until(d =>
            {
                try
                {
                    return condition(d);
                }
                // While a cold-started app loads its first screen, UiAutomator2 may fail to read the window at all.
                // That only means "not there yet", so keep polling.
                catch (UnknownErrorException ex) when (ex.Message.Contains("root AccessibilityNodeInfo"))
                {
                    return default!;
                }
            });
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
