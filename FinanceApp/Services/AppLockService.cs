using System.Security.Cryptography;

namespace FinanceApp.Services;

public class AppLockService : IAppLockService
{
    public const int PinLength = 4;
    public static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);

    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan FirstLockout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxLockout = TimeSpan.FromHours(1);

    private const string PinHashKey = "AppLockPinHash";
    private const string BiometricKey = "AppLockBiometricEnabled";
    private const string HideInRecentsKey = "AppLockHideInRecents";
    private const string FailedAttemptsKey = "AppLockFailedAttempts";
    private const string LockedUntilKey = "AppLockLockedUntil";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly ISecureStorageService _secureStorage;
    private readonly IPreferencesService _preferences;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset? _backgroundedAt;

    public AppLockService(ISecureStorageService secureStorage, IPreferencesService preferences, TimeProvider timeProvider)
    {
        _secureStorage = secureStorage;
        _preferences = preferences;
        _timeProvider = timeProvider;
    }

    // The PIN being on is derived from the stored hash alone, so a backup restored without its secure storage
    // can never leave the app locked with no PIN able to open it.
    public async Task<bool> IsPinEnabledAsync() =>
        !string.IsNullOrEmpty(await _secureStorage.GetAsync(PinHashKey));

    public async Task SetPinAsync(string pin)
    {
        if (!IsValidPin(pin))
            throw new ArgumentException($"The PIN must have exactly {PinLength} digits.", nameof(pin));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        await _secureStorage.SetAsync(PinHashKey, $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(Hash(pin, salt))}");
        ResetFailedAttempts();
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (GetRemainingLockout() > TimeSpan.Zero)
            return false;

        var stored = await _secureStorage.GetAsync(PinHashKey);
        if (string.IsNullOrEmpty(stored))
            return false;

        var parts = stored.Split(':');
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var matches = IsValidPin(pin) && CryptographicOperations.FixedTimeEquals(Hash(pin, salt), expected);

        if (matches)
            ResetFailedAttempts();
        else
            RegisterFailedAttempt();

        return matches;
    }

    public TimeSpan GetRemainingLockout()
    {
        if (!long.TryParse(_preferences.Get(LockedUntilKey, string.Empty), out var lockedUntilMs))
            return TimeSpan.Zero;

        var remaining = DateTimeOffset.FromUnixTimeMilliseconds(lockedUntilMs) - _timeProvider.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public void Disable()
    {
        _secureStorage.Remove(PinHashKey);
        SetBiometricEnabled(false);
        ResetFailedAttempts();
    }

    public bool IsBiometricEnabled => _preferences.Get(BiometricKey, false);

    public void SetBiometricEnabled(bool enabled) => _preferences.Set(BiometricKey, enabled);

    public bool IsHideInRecentsEnabled => _preferences.Get(HideInRecentsKey, true);

    public void SetHideInRecentsEnabled(bool enabled) => _preferences.Set(HideInRecentsKey, enabled);

    public async Task<bool> ShouldHideInRecentsAsync() => IsHideInRecentsEnabled && await IsPinEnabledAsync();

    // The attempts live in preferences, so killing the app doesn't reset them. From the fifth wrong PIN in a row,
    // each one locks the keypad for 30 s, doubling every time up to an hour.
    private void RegisterFailedAttempt()
    {
        int.TryParse(_preferences.Get(FailedAttemptsKey, string.Empty), out var failures);
        failures++;
        _preferences.Set(FailedAttemptsKey, failures.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (failures < MaxFailedAttempts)
            return;

        var doublings = Math.Min(failures - MaxFailedAttempts, 16);
        var lockout = TimeSpan.FromTicks(Math.Min(FirstLockout.Ticks << doublings, MaxLockout.Ticks));
        var lockedUntil = _timeProvider.GetUtcNow() + lockout;
        _preferences.Set(LockedUntilKey, lockedUntil.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private void ResetFailedAttempts()
    {
        _preferences.Set(FailedAttemptsKey, string.Empty);
        _preferences.Set(LockedUntilKey, string.Empty);
    }

    public void OnAppBackgrounded() => _backgroundedAt = _timeProvider.GetUtcNow();

    public async Task<bool> ShouldLockOnResumeAsync()
    {
        if (_backgroundedAt is not { } backgroundedAt)
            return false;

        _backgroundedAt = null;
        return _timeProvider.GetUtcNow() - backgroundedAt >= LockTimeout && await IsPinEnabledAsync();
    }

    private static bool IsValidPin(string pin) => pin.Length == PinLength && pin.All(char.IsAsciiDigit);

    private static byte[] Hash(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
}
