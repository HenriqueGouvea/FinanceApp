using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// "Sobre o Mensal": the version, the privacy policy and terms of use (published on GitHub Pages, from /docs), the
/// developer's contact and the open source notices.
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    public const string SiteUrl = "https://henriquegouvea.github.io/FinanceApp/";
    public const string PrivacyPolicyUrl = SiteUrl + "privacidade/";
    public const string TermsOfUseUrl = SiteUrl + "termos/";
    public const string DeveloperName = "Henrique Gouvêa";
    public const string ContactEmail = "henriquegouvea90@gmail.com";
    public const string NoticesFileName = "open_source_notices.txt";

    private readonly IAppInfo _appInfo;
    private readonly IBrowser _browser;
    private readonly ILauncher _launcher;
    private readonly IFileSystem _fileSystem;
    private readonly IAlertService _alertService;

    [ObservableProperty] string notices = string.Empty;

    public AboutViewModel(IAppInfo appInfo, IBrowser browser, ILauncher launcher, IFileSystem fileSystem, IAlertService alertService)
    {
        _appInfo = appInfo;
        _browser = browser;
        _launcher = launcher;
        _fileSystem = fileSystem;
        _alertService = alertService;
    }

    /// <summary>
    /// "Versão 1.0.0 (12)": the version name and, in parentheses, the build number Google Play counts.
    /// </summary>
    public string VersionText => $"Versão {_appInfo.VersionString} ({_appInfo.BuildString})";

    public string Contact => ContactEmail;
    public string Developer => DeveloperName;

    [RelayCommand]
    Task OpenPrivacyPolicy() => OpenPageAsync(PrivacyPolicyUrl);

    [RelayCommand]
    Task OpenTermsOfUse() => OpenPageAsync(TermsOfUseUrl);

    /// <summary>
    /// Opens a new e-mail to the developer. Without an e-mail app, shows the address so it can be typed elsewhere.
    /// </summary>
    [RelayCommand]
    async Task WriteToDeveloper()
    {
        var subject = Uri.EscapeDataString($"Mensal {_appInfo.VersionString}");
        if (!await _launcher.TryOpenAsync(new Uri($"mailto:{ContactEmail}?subject={subject}")))
            await _alertService.ShowAsync("Contato", $"Escreva para {ContactEmail}.");
    }

    /// <summary>
    /// Reads the notices bundled with the app (Resources/Raw), once.
    /// </summary>
    public async Task LoadNoticesAsync()
    {
        if (Notices.Length > 0)
            return;

        await using var stream = await _fileSystem.OpenAppPackageFileAsync(NoticesFileName);
        using var reader = new StreamReader(stream);
        Notices = await reader.ReadToEndAsync();
    }

    // Opening the page in the browser needs a connection; without a browser app, the address is shown instead.
    private async Task OpenPageAsync(string url)
    {
        try
        {
            await _browser.OpenAsync(new Uri(url), new BrowserLaunchOptions { LaunchMode = BrowserLaunchMode.SystemPreferred });
        }
        catch (Exception)
        {
            await _alertService.ShowAsync("Não foi possível abrir", $"Acesse {url} no navegador.");
        }
    }
}
