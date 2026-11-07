namespace FinanceApp.Services;

/// <summary>
/// Protects the app with a local PIN (and optionally the device biometrics). There is no account yet,
/// so a forgotten PIN can't be recovered.
/// </summary>
public interface IAppLockService
{
    Task<bool> IsPinEnabledAsync();
    Task SetPinAsync(string pin);
    /// <summary>
    /// Checks the PIN and counts a wrong one. After <see cref="AppLockService.MaxFailedAttempts"/> wrong PINs in a row
    /// the keypad locks for a while (see <see cref="GetRemainingLockout"/>), during which every PIN is refused.
    /// </summary>
    Task<bool> VerifyPinAsync(string pin);

    /// <summary>
    /// How long the keypad stays locked after too many wrong PINs; zero when it isn't locked.
    /// </summary>
    TimeSpan GetRemainingLockout();

    /// <summary>
    /// Removes the PIN, which also turns off biometric unlock.
    /// </summary>
    void Disable();

    bool IsBiometricEnabled { get; }
    void SetBiometricEnabled(bool enabled);

    /// <summary>
    /// The "Ocultar conteúdo nos apps recentes" switch. On by default; it only takes effect while the PIN is on.
    /// </summary>
    bool IsHideInRecentsEnabled { get; }
    void SetHideInRecentsEnabled(bool enabled);
    Task<bool> ShouldHideInRecentsAsync();

    void OnAppBackgrounded();

    /// <summary>
    /// True when the PIN is on and the app spent at least <see cref="AppLockService.LockTimeout"/> in the background.
    /// </summary>
    Task<bool> ShouldLockOnResumeAsync();
}
