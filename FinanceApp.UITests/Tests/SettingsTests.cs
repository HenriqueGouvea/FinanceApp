using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Configurações": the user's name and photo, the app lock and the setup wizard, grouped by subject.
/// Re-running the wizard from here is covered by <see cref="SetupTests"/>.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SettingsTests : E2ETestBase
{
    public SettingsTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public void OpenSettings_ShowsSectionsAndInvitesToAddName()
    {
        var settings = MainPage.OpenSettings();

        settings.SectionHeaders.Should().Equal(AppLabels.SettingsUserSection, AppLabels.SettingsSecuritySection, AppLabels.SettingsGuideSection);
        settings.ProfileNameText.Should().Be(AppLabels.AddNameAndPhoto);
    }

    [Fact]
    public void EditName_ShowsInSettingsAndMenuGreeting()
    {
        var settings = MainPage.OpenSettings().EditName("Ana");

        settings.ProfileNameText.Should().Be("Ana");
        settings.GoToHome().ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));
    }

    [Fact]
    public void EditName_KeptAfterRestart()
    {
        MainPage.OpenSettings().EditName("Ana");

        Session.Relaunch();

        new MainPageObject(Session).WaitUntilLoaded().ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting("Ana"));
    }

    [Fact]
    public void OpenSecurity_FromSettings_ShowsPinLockAndBackReturnsHome()
    {
        var security = MainPage.OpenSettings().OpenSecurity();

        security.IsPinLockOn.Should().BeFalse();
        security.GoToEntries().IsLoaded.Should().BeTrue();
    }
}
