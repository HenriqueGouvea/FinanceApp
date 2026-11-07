namespace FinanceApp.Services;

/// <summary>
/// Hides the app's screen in the recent apps list, where the last screen (balances included) would otherwise show.
/// </summary>
public interface IRecentsPrivacyService
{
    void SetHidden(bool hidden);
}
