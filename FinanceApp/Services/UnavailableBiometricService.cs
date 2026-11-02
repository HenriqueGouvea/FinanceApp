namespace FinanceApp.Services;

/// <summary>
/// Used on platforms without a biometric implementation yet, so the PIN stays the only way to unlock.
/// </summary>
public class UnavailableBiometricService : IBiometricService
{
    public bool IsAvailable() => false;

    public Task<bool> AuthenticateAsync(string title, string cancelText) => Task.FromResult(false);
}
