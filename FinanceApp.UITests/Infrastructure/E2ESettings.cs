namespace FinanceApp.UITests.Infrastructure;

public static class E2ESettings
{
    public const string AppPackage = "com.companyname.financeapp";
    public const string DatabaseFileName = "FinanceDataV3.db3";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Each poll is a round trip to the device anyway, so a short interval costs little and avoids idling after the UI is ready.
    /// </summary>
    public static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(100);

    public static string ProjectDirectory { get; } = FindProjectDirectory();

    /// <summary>
    /// Url of an already running Appium server. When unset, the suite starts the locally installed one (npm install).
    /// </summary>
    public static string? AppiumServerUrl => Environment.GetEnvironmentVariable("E2E_APPIUM_URL");

    public static string DeviceName => Environment.GetEnvironmentVariable("E2E_DEVICE") ?? "emulator-5554";

    public static string ApkPath => Environment.GetEnvironmentVariable("E2E_APK_PATH")
        ?? Path.Combine(ProjectDirectory, "..", "artifacts", "e2e-apk", $"{AppPackage}-Signed.apk");

    /// <summary>
    /// UiAutomator2 needs the Android SDK location. Falls back to the default Visual Studio / Android Studio install paths on Windows.
    /// </summary>
    public static string? AndroidHome =>
        Environment.GetEnvironmentVariable("ANDROID_HOME")
        ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
        ?? new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk")
        }.FirstOrDefault(p => Directory.Exists(Path.Combine(p, "platform-tools")));

    public static string AdbPath => AndroidHome is { } androidHome
        ? Path.Combine(androidHome, "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb")
        : "adb";

    private static string FindProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FinanceApp.UITests.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("FinanceApp.UITests project directory not found.");
    }
}
