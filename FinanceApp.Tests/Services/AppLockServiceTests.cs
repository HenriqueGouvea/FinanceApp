using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.Services;

public class AppLockServiceTests
{
    private readonly InMemorySecureStorage _secureStorage = new();
    private readonly InMemoryPreferences _preferences = new();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly AppLockService _service;
    private DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    public AppLockServiceTests()
    {
        _timeProvider.GetUtcNow().Returns(_ => _now);
        _service = new AppLockService(_secureStorage, _preferences, _timeProvider);
    }

    // --- SetPinAsync / IsPinEnabledAsync ---

    [Fact]
    public async Task IsPinEnabledAsync_FreshInstall_ReturnsFalse()
    {
        (await _service.IsPinEnabledAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task SetPinAsync_ValidPin_EnablesLock()
    {
        await _service.SetPinAsync("1234");

        (await _service.IsPinEnabledAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SetPinAsync_ValidPin_StoresHashNotPlainPin()
    {
        await _service.SetPinAsync("1234");

        var stored = await _secureStorage.GetAsync("AppLockPinHash");
        stored.Should().NotBeNullOrEmpty().And.NotContain("1234");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12a4")]
    [InlineData("")]
    public async Task SetPinAsync_NotFourDigits_Throws(string pin)
    {
        var act = () => _service.SetPinAsync(pin);

        await act.Should().ThrowAsync<ArgumentException>();
        (await _service.IsPinEnabledAsync()).Should().BeFalse();
    }

    // --- VerifyPinAsync ---

    [Fact]
    public async Task VerifyPinAsync_CorrectPin_ReturnsTrue()
    {
        await _service.SetPinAsync("2580");

        (await _service.VerifyPinAsync("2580")).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPinAsync_WrongPin_ReturnsFalse()
    {
        await _service.SetPinAsync("2580");

        (await _service.VerifyPinAsync("0852")).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPinAsync_NoPinSet_ReturnsFalse()
    {
        (await _service.VerifyPinAsync("0000")).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPinAsync_AfterPinChanged_OnlyNewPinMatches()
    {
        await _service.SetPinAsync("1111");
        await _service.SetPinAsync("2222");

        (await _service.VerifyPinAsync("1111")).Should().BeFalse();
        (await _service.VerifyPinAsync("2222")).Should().BeTrue();
    }

    // --- Disable ---

    [Fact]
    public async Task Disable_PinAndBiometricsOn_TurnsBothOff()
    {
        await _service.SetPinAsync("1234");
        _service.SetBiometricEnabled(true);

        _service.Disable();

        (await _service.IsPinEnabledAsync()).Should().BeFalse();
        _service.IsBiometricEnabled.Should().BeFalse();
        (await _service.VerifyPinAsync("1234")).Should().BeFalse();
    }

    // --- SetBiometricEnabled ---

    [Fact]
    public void SetBiometricEnabled_True_IsPersisted()
    {
        _service.SetBiometricEnabled(true);

        new AppLockService(_secureStorage, _preferences, _timeProvider).IsBiometricEnabled.Should().BeTrue();
    }

    // --- ShouldLockOnResumeAsync ---

    [Fact]
    public async Task ShouldLockOnResumeAsync_PinOnAndAwayForOneMinute_ReturnsTrue()
    {
        await _service.SetPinAsync("1234");
        _service.OnAppBackgrounded();
        _now += AppLockService.LockTimeout;

        (await _service.ShouldLockOnResumeAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldLockOnResumeAsync_PinOnAndAwayLessThanOneMinute_ReturnsFalse()
    {
        await _service.SetPinAsync("1234");
        _service.OnAppBackgrounded();
        _now += TimeSpan.FromSeconds(59);

        (await _service.ShouldLockOnResumeAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldLockOnResumeAsync_PinOff_ReturnsFalse()
    {
        _service.OnAppBackgrounded();
        _now += TimeSpan.FromMinutes(10);

        (await _service.ShouldLockOnResumeAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldLockOnResumeAsync_NeverBackgrounded_ReturnsFalse()
    {
        await _service.SetPinAsync("1234");
        _now += TimeSpan.FromMinutes(10);

        (await _service.ShouldLockOnResumeAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldLockOnResumeAsync_SecondResumeWithoutBackgrounding_ReturnsFalse()
    {
        await _service.SetPinAsync("1234");
        _service.OnAppBackgrounded();
        _now += TimeSpan.FromMinutes(2);
        await _service.ShouldLockOnResumeAsync();

        (await _service.ShouldLockOnResumeAsync()).Should().BeFalse();
    }

    // --- Failed attempts ---

    [Fact]
    public async Task VerifyPinAsync_FourWrongPins_DoesNotLock()
    {
        await _service.SetPinAsync("1234");

        await FailAsync(4);

        _service.GetRemainingLockout().Should().Be(TimeSpan.Zero);
        (await _service.VerifyPinAsync("1234")).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPinAsync_FifthWrongPin_LocksFor30SecondsEvenWithTheRightPin()
    {
        await _service.SetPinAsync("1234");

        await FailAsync(5);

        _service.GetRemainingLockout().Should().Be(TimeSpan.FromSeconds(30));
        (await _service.VerifyPinAsync("1234")).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPinAsync_LockoutOver_AcceptsTheRightPinAndResetsTheCount()
    {
        await _service.SetPinAsync("1234");
        await FailAsync(5);

        _now += TimeSpan.FromSeconds(30);

        _service.GetRemainingLockout().Should().Be(TimeSpan.Zero);
        (await _service.VerifyPinAsync("1234")).Should().BeTrue();
        await FailAsync(4);
        _service.GetRemainingLockout().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task VerifyPinAsync_EachFurtherWrongPin_DoublesTheWait()
    {
        await _service.SetPinAsync("1234");
        await FailAsync(5);

        _now += TimeSpan.FromSeconds(30);
        await FailAsync(1);
        _service.GetRemainingLockout().Should().Be(TimeSpan.FromSeconds(60));

        _now += TimeSpan.FromSeconds(60);
        await FailAsync(1);
        _service.GetRemainingLockout().Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public async Task VerifyPinAsync_ManyWrongPins_WaitsAtMostAnHour()
    {
        await _service.SetPinAsync("1234");

        for (var i = 0; i < 30; i++)
        {
            await FailAsync(1);
            _now += _service.GetRemainingLockout();
        }

        await FailAsync(1);
        _service.GetRemainingLockout().Should().Be(AppLockService.MaxLockout);
    }

    [Fact]
    public async Task VerifyPinAsync_AppRestarted_KeepsTheLockout()
    {
        await _service.SetPinAsync("1234");
        await FailAsync(5);

        var restarted = new AppLockService(_secureStorage, _preferences, _timeProvider);

        restarted.GetRemainingLockout().Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task Disable_AfterWrongPins_ResetsTheCount()
    {
        await _service.SetPinAsync("1234");
        await FailAsync(4);

        _service.Disable();
        await _service.SetPinAsync("1234");
        await FailAsync(1);

        _service.GetRemainingLockout().Should().Be(TimeSpan.Zero);
    }

    // --- Hide in recents ---

    [Fact]
    public async Task ShouldHideInRecentsAsync_PinOnByDefault_ReturnsTrue()
    {
        await _service.SetPinAsync("1234");

        _service.IsHideInRecentsEnabled.Should().BeTrue();
        (await _service.ShouldHideInRecentsAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldHideInRecentsAsync_NoPin_ReturnsFalse()
    {
        (await _service.ShouldHideInRecentsAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldHideInRecentsAsync_SwitchedOff_ReturnsFalse()
    {
        await _service.SetPinAsync("1234");

        _service.SetHideInRecentsEnabled(false);

        (await _service.ShouldHideInRecentsAsync()).Should().BeFalse();
    }

    private async Task FailAsync(int times)
    {
        for (var i = 0; i < times; i++)
            await _service.VerifyPinAsync("0000");
    }
}
