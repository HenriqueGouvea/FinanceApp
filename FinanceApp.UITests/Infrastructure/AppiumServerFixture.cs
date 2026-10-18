using OpenQA.Selenium.Appium.Service;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Shared across the whole E2E collection: starting Appium takes several seconds, sessions are cheap.
/// </summary>
public sealed class AppiumServerFixture : IDisposable
{
    private readonly AppiumLocalService? _localService;

    public Uri ServerUri { get; }

    /// <summary>
    /// A Fast Deployment install (dotnet build -t:Run) shares package name and version code with the E2E APK,
    /// so Appium would skip installing it. Force the install once per run, then reuse it.
    /// </summary>
    public bool IsAppInstalled { get; set; }

    public AppiumServerFixture()
    {
        if (E2ESettings.AppiumServerUrl is { } url)
        {
            ServerUri = new Uri(url);
            return;
        }

        var appiumJs = new FileInfo(Path.Combine(E2ESettings.ProjectDirectory, "node_modules", "appium", "index.js"));
        if (!appiumJs.Exists)
            throw new InvalidOperationException(
                $"Appium not found at {appiumJs.FullName}. Run 'npm install' inside FinanceApp.UITests or set E2E_APPIUM_URL.");

        var environment = new Dictionary<string, string> { ["APPIUM_HOME"] = E2ESettings.ProjectDirectory };
        if (E2ESettings.AndroidHome is { } androidHome)
            environment["ANDROID_HOME"] = androidHome;

        _localService = new AppiumServiceBuilder()
            .WithAppiumJS(appiumJs)
            .UsingAnyFreePort()
            .WithEnvironment(environment)
            .Build();
        _localService.Start();

        ServerUri = _localService.ServiceUrl;
    }

    public void Dispose() => _localService?.Dispose();
}

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<AppiumServerFixture>
{
    public const string Name = "E2E";
}
