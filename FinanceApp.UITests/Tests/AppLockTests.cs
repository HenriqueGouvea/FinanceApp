using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The PIN lock set up on the "Segurança" page. Biometric unlock isn't covered: the emulator has no enrolled fingerprint.
/// The lock after a minute in the background is covered by the AppLockService unit tests instead of waiting it out here.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AppLockTests : E2ETestBase
{
    private const string Pin = "2580";

    public AppLockTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public void OpenSecurity_FreshInstall_PinOffWithNoAccountNotice()
    {
        var security = MainPage.OpenSecurity();

        security.IsNoAccountNoticeVisible.Should().BeTrue();
        security.IsPinLockOn.Should().BeFalse();
        security.IsChangePinVisible.Should().BeFalse();
        security.IsBiometricOptionVisible.Should().BeFalse();
    }

    [Fact]
    public void EnablePin_MatchingConfirmation_TurnsLockOnAndOffersChangeAndBiometrics()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        security.IsPinLockOn.Should().BeTrue();
        security.IsChangePinVisible.Should().BeTrue();
        security.IsBiometricOptionVisible.Should().BeTrue();
    }

    [Fact]
    public void EnablePin_KeypadShowsTypedDigitsAndCanBeCancelled()
    {
        var security = MainPage.OpenSecurity();
        var keypad = security.TapPinLock().WaitForTitle(AppLabels.CreatePin);

        keypad.IsCancelVisible.Should().BeTrue();
        keypad.IsForgotPinVisible.Should().BeFalse();
        keypad.Type("12").DotsText.Should().Be("● ● ○ ○");
        keypad.DeleteDigit().DotsText.Should().Be("● ○ ○ ○");

        keypad.Cancel();

        security.IsPinLockOn.Should().BeFalse();
    }

    [Fact]
    public void EnablePin_MismatchedConfirmation_AsksForNewPinAgain()
    {
        var security = MainPage.OpenSecurity();
        var keypad = security.TapPinLock()
            .Type(Pin)
            .WaitForTitle(AppLabels.ConfirmPin)
            .Type("1111");

        keypad.WaitForTitle(AppLabels.CreatePin).WaitForError(AppLabels.PinMismatch);

        keypad.Type("1111").WaitForTitle(AppLabels.ConfirmPin).TypeAndWaitForClose("1111");
        security.WaitForPinLock(true);
    }

    [Fact]
    public void Relaunch_PinEnabled_LockScreenUnlocksWithCorrectPin()
    {
        MainPage.OpenSecurity().EnablePin(Pin);

        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();

        lockScreen.TitleText.Should().Be(AppLabels.EnterPin);
        lockScreen.IsCancelVisible.Should().BeFalse();
        lockScreen.IsForgotPinVisible.Should().BeTrue();

        lockScreen.TypeAndWaitForClose(Pin);
        new MainPageObject(Session).WaitUntilLoaded();
    }

    [Fact]
    public void Relaunch_WrongPin_ShowsErrorAndStaysLocked()
    {
        MainPage.OpenSecurity().EnablePin(Pin);

        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded().Type("0000");

        lockScreen.WaitForError(AppLabels.WrongPin);
        lockScreen.DotsText.Should().Be("○ ○ ○ ○");
        lockScreen.IsVisible.Should().BeTrue();
        new MainPageObject(Session).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void LockScreen_PressBack_LeavesAppAndStaysLocked()
    {
        MainPage.OpenSecurity().EnablePin(Pin);
        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();

        Session.PressBack();
        Session.BringToForeground();

        lockScreen.WaitUntilLoaded().TypeAndWaitForClose(Pin);
        new MainPageObject(Session).WaitUntilLoaded();
    }

    [Fact]
    public void LockScreen_ForgotPin_ExplainsPinCannotBeRecovered()
    {
        MainPage.OpenSecurity().EnablePin(Pin);
        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();

        lockScreen.ForgotPinExplainsNoRecovery().Should().BeTrue();
        lockScreen.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Background_LessThanAMinute_DoesNotLock()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        Session.SendToBackground(TimeSpan.FromSeconds(3));

        security.WaitUntilLoaded();
        new PinPageObject(Session).IsVisible.Should().BeFalse();
    }

    [Fact]
    public void DisablePin_CorrectPin_TurnsLockOffAndRelaunchIsNotLocked()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        security.TapPinLock().WaitForTitle(AppLabels.EnterCurrentPin).TypeAndWaitForClose(Pin);

        security.WaitForPinLock(false);
        security.IsChangePinVisible.Should().BeFalse();

        Session.Relaunch();
        new MainPageObject(Session).WaitUntilLoaded();
        new PinPageObject(Session).IsVisible.Should().BeFalse();
    }

    [Fact]
    public void DisablePin_WrongPinThenCancel_KeepsLockOn()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        security.TapPinLock().Type("0000").WaitForError(AppLabels.WrongPin).Cancel();

        security.IsPinLockOn.Should().BeTrue();
    }

    [Fact]
    public void ChangePin_NewPinUnlocksAndOldOneNoLonger()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        security.TapChangePin()
            .WaitForTitle(AppLabels.EnterCurrentPin)
            .Type(Pin)
            .WaitForTitle(AppLabels.EnterNewPin)
            .Type("1397")
            .WaitForTitle(AppLabels.ConfirmPin)
            .TypeAndWaitForClose("1397");

        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();
        lockScreen.Type(Pin).WaitForError(AppLabels.WrongPin);
        lockScreen.TypeAndWaitForClose("1397");
        new MainPageObject(Session).WaitUntilLoaded();
    }
}
