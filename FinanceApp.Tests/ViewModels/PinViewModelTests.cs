using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PinViewModelTests
{
    private readonly IAppLockService _appLock;
    private readonly IBiometricService _biometrics;
    private readonly PinViewModel _vm;
    private int _completedCount;

    public PinViewModelTests()
    {
        _appLock = Substitute.For<IAppLockService>();
        _biometrics = Substitute.For<IBiometricService>();
        _appLock.VerifyPinAsync("1234").Returns(true);
        _vm = new PinViewModel(_appLock, _biometrics);
        _vm.SetCompletedCallback(() =>
        {
            _completedCount++;
            return Task.CompletedTask;
        });
    }

    private async Task TypeAsync(string digits)
    {
        foreach (var digit in digits)
            await _vm.AddDigitCommand.ExecuteAsync(digit.ToString());
    }

    // --- Prepare ---

    [Theory]
    [InlineData(PinPurpose.Unlock, "Digite seu PIN", false)]
    [InlineData(PinPurpose.Create, "Crie um PIN de 4 dígitos", true)]
    [InlineData(PinPurpose.Change, "Digite o PIN atual", true)]
    [InlineData(PinPurpose.Disable, "Digite o PIN atual", true)]
    public void Prepare_EachPurpose_SetsTitleAndCancel(PinPurpose purpose, string title, bool canCancel)
    {
        _vm.Prepare(purpose);

        _vm.Title.Should().Be(title);
        _vm.CanCancel.Should().Be(canCancel);
        _vm.Dots.Should().Be("○ ○ ○ ○");
    }

    [Fact]
    public void Prepare_UnlockWithBiometricsOnAndAvailable_OffersBiometrics()
    {
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);

        _vm.Prepare(PinPurpose.Unlock);

        _vm.CanUseBiometrics.Should().BeTrue();
    }

    [Fact]
    public void Prepare_BiometricsOnButNoneEnrolled_HidesBiometrics()
    {
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(false);

        _vm.Prepare(PinPurpose.Unlock);

        _vm.CanUseBiometrics.Should().BeFalse();
    }

    [Fact]
    public void Prepare_DisableWithBiometricsOn_HidesBiometrics()
    {
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);

        _vm.Prepare(PinPurpose.Disable);

        _vm.CanUseBiometrics.Should().BeFalse();
    }

    // --- AddDigit / DeleteDigit ---

    [Fact]
    public async Task AddDigit_TwoDigits_FillsTwoDots()
    {
        _vm.Prepare(PinPurpose.Unlock);

        await TypeAsync("12");

        _vm.Dots.Should().Be("● ● ○ ○");
    }

    [Fact]
    public async Task DeleteDigit_AfterTwoDigits_RemovesLast()
    {
        _vm.Prepare(PinPurpose.Unlock);
        await TypeAsync("12");

        _vm.DeleteDigitCommand.Execute(null);

        _vm.Pin.Should().Be("1");
    }

    [Fact]
    public void DeleteDigit_Empty_DoesNothing()
    {
        _vm.Prepare(PinPurpose.Unlock);

        _vm.DeleteDigitCommand.Execute(null);

        _vm.Pin.Should().BeEmpty();
    }

    // --- Unlock ---

    [Fact]
    public async Task Unlock_CorrectPin_Completes()
    {
        _vm.Prepare(PinPurpose.Unlock);

        await TypeAsync("1234");

        _completedCount.Should().Be(1);
    }

    [Fact]
    public async Task Unlock_WrongPin_ShowsErrorClearsAndStays()
    {
        _vm.Prepare(PinPurpose.Unlock);

        await TypeAsync("9999");

        _completedCount.Should().Be(0);
        _vm.ErrorMessage.Should().Be("PIN incorreto. Tente novamente.");
        _vm.Pin.Should().BeEmpty();
        _vm.Title.Should().Be("Digite seu PIN");
    }

    [Fact]
    public async Task Unlock_DigitAfterError_ClearsError()
    {
        _vm.Prepare(PinPurpose.Unlock);
        await TypeAsync("9999");

        await TypeAsync("1");

        _vm.ErrorMessage.Should().BeEmpty();
    }

    // --- UnlockWithBiometrics ---

    [Fact]
    public async Task UnlockWithBiometrics_Authenticated_Completes()
    {
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);
        _biometrics.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _vm.Prepare(PinPurpose.Unlock);

        await _vm.UnlockWithBiometricsCommand.ExecuteAsync(null);

        _completedCount.Should().Be(1);
    }

    [Fact]
    public async Task UnlockWithBiometrics_Cancelled_StaysLocked()
    {
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);
        _biometrics.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        _vm.Prepare(PinPurpose.Unlock);

        await _vm.UnlockWithBiometricsCommand.ExecuteAsync(null);

        _completedCount.Should().Be(0);
    }

    [Fact]
    public async Task UnlockWithBiometrics_BiometricsOff_NeverPrompts()
    {
        _vm.Prepare(PinPurpose.Unlock);

        await _vm.UnlockWithBiometricsCommand.ExecuteAsync(null);

        await _biometrics.DidNotReceiveWithAnyArgs().AuthenticateAsync(default!, default!);
        _completedCount.Should().Be(0);
    }

    // --- Create ---

    [Fact]
    public async Task Create_FirstPin_AsksForConfirmation()
    {
        _vm.Prepare(PinPurpose.Create);

        await TypeAsync("4321");

        _vm.Title.Should().Be("Confirme o PIN");
        _vm.Pin.Should().BeEmpty();
        await _appLock.DidNotReceiveWithAnyArgs().SetPinAsync(default!);
    }

    [Fact]
    public async Task Create_MatchingConfirmation_SavesPinAndCompletes()
    {
        _vm.Prepare(PinPurpose.Create);

        await TypeAsync("4321");
        await TypeAsync("4321");

        await _appLock.Received(1).SetPinAsync("4321");
        _completedCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_MismatchedConfirmation_RestartsWithErrorAndSavesNothing()
    {
        _vm.Prepare(PinPurpose.Create);

        await TypeAsync("4321");
        await TypeAsync("1111");

        _vm.Title.Should().Be("Crie um PIN de 4 dígitos");
        _vm.ErrorMessage.Should().Be("Os PINs não conferem. Crie o PIN novamente.");
        await _appLock.DidNotReceiveWithAnyArgs().SetPinAsync(default!);
        _completedCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_MismatchThenMatch_SavesSecondPin()
    {
        _vm.Prepare(PinPurpose.Create);

        await TypeAsync("4321");
        await TypeAsync("1111");
        await TypeAsync("5678");
        await TypeAsync("5678");

        await _appLock.Received(1).SetPinAsync("5678");
        _completedCount.Should().Be(1);
    }

    // --- Change ---

    [Fact]
    public async Task Change_CorrectCurrentPin_AsksForNewPin()
    {
        _vm.Prepare(PinPurpose.Change);

        await TypeAsync("1234");

        _vm.Title.Should().Be("Digite o novo PIN");
        _completedCount.Should().Be(0);
    }

    [Fact]
    public async Task Change_FullFlow_SavesNewPin()
    {
        _vm.Prepare(PinPurpose.Change);

        await TypeAsync("1234");
        await TypeAsync("9876");
        await TypeAsync("9876");

        await _appLock.Received(1).SetPinAsync("9876");
        _completedCount.Should().Be(1);
    }

    [Fact]
    public async Task Change_WrongCurrentPin_StaysOnCurrentPinStep()
    {
        _vm.Prepare(PinPurpose.Change);

        await TypeAsync("0000");

        _vm.Title.Should().Be("Digite o PIN atual");
        _vm.ErrorMessage.Should().Be("PIN incorreto. Tente novamente.");
    }

    // --- Disable ---

    [Fact]
    public async Task Disable_CorrectPin_DisablesLockAndCompletes()
    {
        _vm.Prepare(PinPurpose.Disable);

        await TypeAsync("1234");

        _appLock.Received(1).Disable();
        _completedCount.Should().Be(1);
    }

    [Fact]
    public async Task Disable_WrongPin_KeepsLock()
    {
        _vm.Prepare(PinPurpose.Disable);

        await TypeAsync("0000");

        _appLock.DidNotReceive().Disable();
        _completedCount.Should().Be(0);
    }
}
