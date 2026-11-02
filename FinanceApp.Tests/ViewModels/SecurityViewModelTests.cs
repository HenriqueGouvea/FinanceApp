using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class SecurityViewModelTests
{
    private readonly IAppLockService _appLock;
    private readonly IBiometricService _biometrics;
    private readonly SecurityViewModel _vm;

    public SecurityViewModelTests()
    {
        _appLock = Substitute.For<IAppLockService>();
        _biometrics = Substitute.For<IBiometricService>();
        _vm = new SecurityViewModel(_appLock, _biometrics);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_ReadsLockState()
    {
        _appLock.IsPinEnabledAsync().Returns(true);
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);

        await _vm.LoadAsync();

        _vm.IsPinEnabled.Should().BeTrue();
        _vm.IsBiometricEnabled.Should().BeTrue();
        _vm.IsBiometricAvailable.Should().BeTrue();
        _vm.BiometricHint.Should().Be("Use a digital ou o rosto em vez do PIN");
    }

    [Fact]
    public async Task LoadAsync_NoBiometricsEnrolled_ExplainsWhy()
    {
        _biometrics.IsAvailable().Returns(false);

        await _vm.LoadAsync();

        _vm.BiometricHint.Should().Be("Nenhuma biometria cadastrada neste aparelho");
    }

    // --- ToggleBiometric ---

    [Fact]
    public async Task ToggleBiometric_OffAndAuthenticated_TurnsOn()
    {
        _appLock.IsPinEnabledAsync().Returns(true);
        _biometrics.IsAvailable().Returns(true);
        _biometrics.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        await _vm.LoadAsync();

        await _vm.ToggleBiometricCommand.ExecuteAsync(null);

        _vm.IsBiometricEnabled.Should().BeTrue();
        _appLock.Received(1).SetBiometricEnabled(true);
    }

    [Fact]
    public async Task ToggleBiometric_OffAndPromptCancelled_StaysOff()
    {
        _appLock.IsPinEnabledAsync().Returns(true);
        _biometrics.IsAvailable().Returns(true);
        _biometrics.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        await _vm.LoadAsync();

        await _vm.ToggleBiometricCommand.ExecuteAsync(null);

        _vm.IsBiometricEnabled.Should().BeFalse();
        _appLock.DidNotReceiveWithAnyArgs().SetBiometricEnabled(default);
    }

    [Fact]
    public async Task ToggleBiometric_On_TurnsOffWithoutPrompt()
    {
        _appLock.IsPinEnabledAsync().Returns(true);
        _appLock.IsBiometricEnabled.Returns(true);
        _biometrics.IsAvailable().Returns(true);
        await _vm.LoadAsync();

        await _vm.ToggleBiometricCommand.ExecuteAsync(null);

        _vm.IsBiometricEnabled.Should().BeFalse();
        _appLock.Received(1).SetBiometricEnabled(false);
        await _biometrics.DidNotReceiveWithAnyArgs().AuthenticateAsync(default!, default!);
    }

    [Fact]
    public async Task ToggleBiometric_NoneEnrolled_DoesNothing()
    {
        _appLock.IsPinEnabledAsync().Returns(true);
        _biometrics.IsAvailable().Returns(false);
        await _vm.LoadAsync();

        await _vm.ToggleBiometricCommand.ExecuteAsync(null);

        _vm.IsBiometricEnabled.Should().BeFalse();
        await _biometrics.DidNotReceiveWithAnyArgs().AuthenticateAsync(default!, default!);
    }

    [Fact]
    public async Task ToggleBiometric_PinOff_DoesNothing()
    {
        _biometrics.IsAvailable().Returns(true);
        await _vm.LoadAsync();

        await _vm.ToggleBiometricCommand.ExecuteAsync(null);

        _appLock.DidNotReceiveWithAnyArgs().SetBiometricEnabled(default);
    }
}
