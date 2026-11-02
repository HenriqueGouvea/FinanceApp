using System.Security.Cryptography;

namespace FinanceApp.Services;

public class AppLockService : IAppLockService
{
    public const int PinLength = 4;
    public static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);

    private const string PinHashKey = "AppLockPinHash";
    private const string BiometricKey = "AppLockBiometricEnabled";
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
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var stored = await _secureStorage.GetAsync(PinHashKey);
        if (string.IsNullOrEmpty(stored) || !IsValidPin(pin))
            return false;

        var parts = stored.Split(':');
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        return CryptographicOperations.FixedTimeEquals(Hash(pin, salt), expected);
    }

    public void Disable()
    {
        _secureStorage.Remove(PinHashKey);
        SetBiometricEnabled(false);
    }

    public bool IsBiometricEnabled => _preferences.Get(BiometricKey, false);

    public void SetBiometricEnabled(bool enabled) => _preferences.Set(BiometricKey, enabled);

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
