using FinanceApp.Models;

namespace FinanceApp.Services;

/// <summary>
/// The setup wizard shown the first time the app opens, which turns the user's salary, accounts and house expenses
/// into their first entries.
/// </summary>
public interface ISetupService
{
    /// <summary>
    /// True on the first run only: the setup was never completed or skipped and there is no data yet, so people who
    /// used the app before the setup existed never see it.
    /// </summary>
    Task<bool> ShouldShowAsync();

    /// <summary>
    /// Saves the profile, creates the payment sources and then one recurrent entry per <see cref="SetupEntry"/>,
    /// starting in the current month. Existing data is kept, so the setup can be run again to add more.
    /// </summary>
    Task CompleteAsync(NewUserSetup setup);

    /// <summary>
    /// Marks the setup as done without creating anything.
    /// </summary>
    void Skip();
}
