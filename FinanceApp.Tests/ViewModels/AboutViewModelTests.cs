using System.Text;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class AboutViewModelTests
{
    private readonly IAppInfo _appInfo = Substitute.For<IAppInfo>();
    private readonly IBrowser _browser = Substitute.For<IBrowser>();
    private readonly ILauncher _launcher = Substitute.For<ILauncher>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IAlertService _alertService = Substitute.For<IAlertService>();
    private readonly AboutViewModel _vm;

    public AboutViewModelTests()
    {
        _appInfo.VersionString.Returns("1.2.0");
        _appInfo.BuildString.Returns("12");
        _vm = new AboutViewModel(_appInfo, _browser, _launcher, _fileSystem, _alertService);
    }

    [Fact]
    public void VersionText_ShowsVersionNameAndBuildNumber()
    {
        _vm.VersionText.Should().Be("Versão 1.2.0 (12)");
    }

    [Fact]
    public void Contact_ShowsDeveloperAndEmail()
    {
        (_vm.Developer, _vm.Contact).Should().Be(("Henrique Gouvêa", "henriquegouvea90@gmail.com"));
    }

    [Fact]
    public async Task OpenPrivacyPolicy_OpensThePolicyPageInTheBrowser()
    {
        await _vm.OpenPrivacyPolicyCommand.ExecuteAsync(null);

        await _browser.Received(1).OpenAsync(
            new Uri("https://henriquegouvea.github.io/FinanceApp/privacidade/"),
            Arg.Is<BrowserLaunchOptions>(o => o.LaunchMode == BrowserLaunchMode.SystemPreferred));
    }

    [Fact]
    public async Task OpenTermsOfUse_OpensTheTermsPageInTheBrowser()
    {
        await _vm.OpenTermsOfUseCommand.ExecuteAsync(null);

        await _browser.Received(1).OpenAsync(new Uri("https://henriquegouvea.github.io/FinanceApp/termos/"), Arg.Any<BrowserLaunchOptions>());
    }

    [Fact]
    public async Task OpenPrivacyPolicy_NoBrowser_ShowsTheAddress()
    {
        _browser.OpenAsync(Arg.Any<Uri>(), Arg.Any<BrowserLaunchOptions>()).ThrowsAsync(new InvalidOperationException());

        await _vm.OpenPrivacyPolicyCommand.ExecuteAsync(null);

        await _alertService.Received(1).ShowAsync("Não foi possível abrir", $"Acesse {AboutViewModel.PrivacyPolicyUrl} no navegador.");
    }

    [Fact]
    public async Task WriteToDeveloper_OpensAnEmailWithTheVersionInTheSubject()
    {
        _launcher.TryOpenAsync(Arg.Any<Uri>()).Returns(true);

        await _vm.WriteToDeveloperCommand.ExecuteAsync(null);

        await _launcher.Received(1).TryOpenAsync(new Uri("mailto:henriquegouvea90@gmail.com?subject=Mensal%201.2.0"));
        await _alertService.DidNotReceive().ShowAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task WriteToDeveloper_NoEmailApp_ShowsTheAddress()
    {
        _launcher.TryOpenAsync(Arg.Any<Uri>()).Returns(false);

        await _vm.WriteToDeveloperCommand.ExecuteAsync(null);

        await _alertService.Received(1).ShowAsync("Contato", "Escreva para henriquegouvea90@gmail.com.");
    }

    [Fact]
    public async Task LoadNoticesAsync_ReadsTheBundledFileOnce()
    {
        _fileSystem.OpenAppPackageFileAsync(AboutViewModel.NoticesFileName)
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("Open Sans · SIL OFL 1.1"))));

        await _vm.LoadNoticesAsync();
        await _vm.LoadNoticesAsync();

        _vm.Notices.Should().Be("Open Sans · SIL OFL 1.1");
        await _fileSystem.Received(1).OpenAppPackageFileAsync(AboutViewModel.NoticesFileName);
    }

    [Fact]
    public void NoticesFile_IsBundledAndCoversEveryComponentLicense()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FinanceApp", "Resources", "Raw", AboutViewModel.NoticesFileName);

        var notices = File.ReadAllText(path);

        notices.Should().ContainAll(
            "Open Sans", "SIL OPEN FONT LICENSE Version 1.1", "MIT License", "Apache License", "Version 2.0",
            "sqlite-net", "SQLitePCLRaw", "CommunityToolkit.Maui", "CommunityToolkit.Mvvm", "SQLite");
    }
}
