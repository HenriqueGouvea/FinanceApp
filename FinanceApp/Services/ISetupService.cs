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
    /// Saves the profile, creates the accounts, vouchers and credit cards and then one entry per <see cref="SetupEntry"/>
    /// from the current month: recurrent, unless it is a credit card purchase made once or in installments. A row the
    /// wizard created before (<see cref="SetupEntry.ExistingEntryId"/>) updates its entry instead, and
    /// <see cref="NewUserSetup.EntriesToRemove"/> are deleted. Each entry is linked to its wizard row, so the wizard can
    /// show it when run again. Everything else is kept.
    /// </summary>
    Task CompleteAsync(NewUserSetup setup);

    /// <summary>
    /// The entries the wizard created before that are still running (not ended, nor finished installments), with the
    /// rows they came from, oldest first, so the wizard run again opens with them.
    /// </summary>
    Task<IReadOnlyList<PreviousSetupEntry>> GetPreviousSetupAsync();

    /// <summary>
    /// Marks the setup as done without creating anything.
    /// </summary>
    void Skip();
}
