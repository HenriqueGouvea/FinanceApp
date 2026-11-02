namespace FinanceApp.Services;

public interface IBiometricService
{
    /// <summary>
    /// True when the device has biometric hardware with at least one fingerprint or face enrolled.
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Shows the system biometric prompt. Returns false when the user cancels it or it fails for good.
    /// </summary>
    Task<bool> AuthenticateAsync(string title, string cancelText);
}
