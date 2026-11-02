namespace FinanceApp.Models;

/// <summary>
/// Why the PIN keypad is open, which sets the steps it walks through.
/// </summary>
public enum PinPurpose
{
    /// <summary>Current PIN (or biometrics) to open the locked app.</summary>
    Unlock,
    /// <summary>New PIN, then its confirmation.</summary>
    Create,
    /// <summary>Current PIN, then the new PIN and its confirmation.</summary>
    Change,
    /// <summary>Current PIN, then the lock is turned off.</summary>
    Disable
}
