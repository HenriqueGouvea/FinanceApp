using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The PIN lock set up on the "Segurança" page. Biometric unlock isn't covered: the emulator has no enrolled fingerprint.
/// The keypad, the lock after a minute in the background and the error rules are covered by the PinViewModel and
/// AppLockService unit tests instead.
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
    public void EnablePin_MismatchThenMatchingConfirmation_TurnsLockOnAndOffersChangeAndBiometrics()
    {
        var security = MainPage.OpenSecurity();
        security.IsNoAccountNoticeVisible.Should().BeTrue();
        security.IsPinLockOn.Should().BeFalse();
        security.IsChangePinVisible.Should().BeFalse();
        security.IsBiometricOptionVisible.Should().BeFalse();

        var keypad = security.TapPinLock()
            .WaitForTitle(AppLabels.CreatePin)
            .Type(Pin)
            .WaitForTitle(AppLabels.ConfirmPin)
            .Type("1111");
        keypad.WaitForTitle(AppLabels.CreatePin).WaitForError(AppLabels.PinMismatch);
        keypad.Type(Pin).WaitForTitle(AppLabels.ConfirmPin).TypeAndWaitForClose(Pin);

        security.WaitForPinLock(true);
        security.IsChangePinVisible.Should().BeTrue();
        security.IsBiometricOptionVisible.Should().BeTrue();
    }

    [Fact]
    [Trait("Smoke", "true")]
    public void Relaunch_PinEnabled_WrongPinStaysLockedAndCorrectPinUnlocks()
    {
        MainPage.OpenSecurity().EnablePin(Pin);

        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();
        lockScreen.TitleText.Should().Be(AppLabels.EnterPin);
        lockScreen.IsCancelVisible.Should().BeFalse();

        lockScreen.Type("0000").WaitForError(AppLabels.WrongPin);
        lockScreen.DotsText.Should().Be("○ ○ ○ ○");
        new MainPageObject(Session).IsLoaded.Should().BeFalse();

        lockScreen.TypeAndWaitForClose(Pin);
        new MainPageObject(Session).WaitUntilLoaded();
    }

    [Fact]
    public void LockScreen_ForgotPinThenPressBack_ExplainsNoRecoveryAndStaysLocked()
    {
        MainPage.OpenSecurity().EnablePin(Pin);
        Session.Relaunch();
        var lockScreen = new PinPageObject(Session).WaitUntilLoaded();

        lockScreen.ForgotPinExplainsNoRecovery().Should().BeTrue();
        lockScreen.IsVisible.Should().BeTrue();

        Session.PressBack();
        Session.BringToForeground();

        lockScreen.WaitUntilLoaded().TypeAndWaitForClose(Pin);
        new MainPageObject(Session).WaitUntilLoaded();
    }

    [Fact]
    public void DisablePin_WrongPinThenCorrectPin_TurnsLockOffAndRelaunchIsNotLocked()
    {
        var security = MainPage.OpenSecurity().EnablePin(Pin);

        security.TapPinLock().Type("0000").WaitForError(AppLabels.WrongPin).Cancel();
        security.IsPinLockOn.Should().BeTrue();

        security.TapPinLock().WaitForTitle(AppLabels.EnterCurrentPin).TypeAndWaitForClose(Pin);
        security.WaitForPinLock(false);
        security.IsChangePinVisible.Should().BeFalse();

        Session.Relaunch();
        new MainPageObject(Session).WaitUntilLoaded();
        new PinPageObject(Session).IsVisible.Should().BeFalse();
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
