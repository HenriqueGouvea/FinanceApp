using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Configurações": the user's name and photo, the app lock, the setup wizard and "Sobre o Mensal", grouped by subject.
/// The policy, terms and contact links of "Sobre o Mensal" open the browser and the e-mail app, outside the app, so
/// they are covered by the AboutViewModel tests.
/// The app lock is covered by <see cref="AppLockTests"/> and re-running the wizard by <see cref="SetupTests"/>,
/// both opened from here.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SettingsTests : E2ETestBase
{
    public SettingsTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public void EditName_ShowsInSettingsAndMenuGreetingAndIsKeptAfterRestart()
    {
        var settings = MainPage.OpenSettings();
        settings.SectionHeaders.Should().Equal(
            AppLabels.SettingsUserSection, AppLabels.SettingsSecuritySection, AppLabels.SettingsGuideSection, AppLabels.SettingsDataSection,
            AppLabels.SettingsAboutSection);
        settings.ProfileNameText.Should().Be(AppLabels.AddNameAndPhoto);

        settings.EditName("Ana");

        settings.ProfileNameText.Should().Be("Ana");
        var home = settings.GoToHome();
        home.HeaderAvatarInitialText.Should().Be("A");
        home.ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));

        Session.Relaunch();

        new MainPageObject(Session).WaitUntilLoaded().ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));
    }

    [Fact]
    public void OpenAbout_ShowsVersionLinksContactAndOpenSourceNotices()
    {
        var about = MainPage.OpenSettings().OpenAbout();

        using (new AssertionScope())
        {
            about.VersionText.Should().MatchRegex(@"^Versão \d+\.\d+\.\d+ \(\d+\)$");
            about.AreLinksVisible.Should().BeTrue();
            about.ContactText.Should().Be(AppLabels.DeveloperContact);
        }

        about.ReadNotices().Should().Contain("Open Sans").And.Contain("SQLite");
    }
}
