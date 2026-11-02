namespace FinanceApp.Services;

/// <summary>
/// Protects the app with a local PIN (and optionally the device biometrics). There is no account yet,
/// so a forgotten PIN can't be recovered.
/// </summary>
public interface IAppLockService
{
    Task<bool> IsPinEnabledAsync();
    Task SetPinAsync(string pin);
    Task<bool> VerifyPinAsync(string pin);

    /// <summary>
    /// Removes the PIN, which also turns off biometric unlock.
    /// </summary>
    void Disable();

    bool IsBiometricEnabled { get; }
    void SetBiometricEnabled(bool enabled);

    void OnAppBackgrounded();

    /// <summary>
    /// True when the PIN is on and the app spent at least <see cref="AppLockService.LockTimeout"/> in the background.
    /// </summary>
    Task<bool> ShouldLockOnResumeAsync();
}
