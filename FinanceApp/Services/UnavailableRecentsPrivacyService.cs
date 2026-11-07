namespace FinanceApp.Services;

/// <summary>
/// Used on platforms without an implementation yet.
/// </summary>
public class UnavailableRecentsPrivacyService : IRecentsPrivacyService
{
    public void SetHidden(bool hidden)
    {
    }
}
