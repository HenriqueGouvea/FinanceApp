using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Configurações": the user's name and photo, the app lock and the setup wizard, grouped by subject.
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
            AppLabels.SettingsUserSection, AppLabels.SettingsSecuritySection, AppLabels.SettingsGuideSection, AppLabels.SettingsDataSection);
        settings.ProfileNameText.Should().Be(AppLabels.AddNameAndPhoto);

        settings.EditName("Ana");

        settings.ProfileNameText.Should().Be("Ana");
        var home = settings.GoToHome();
        home.HeaderAvatarInitialText.Should().Be("A");
        home.ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));

        Session.Relaunch();

        new MainPageObject(Session).WaitUntilLoaded().ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));
    }
}
