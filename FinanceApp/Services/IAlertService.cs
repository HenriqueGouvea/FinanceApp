namespace FinanceApp.Services;

public interface IAlertService
{
    /// <summary>
    /// Shows a message with a single "OK" button over the page on top.
    /// </summary>
    Task ShowAsync(string title, string message);

    /// <summary>
    /// Asks the user to confirm over the page on top; true when they tap <paramref name="accept"/>.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>
    /// Asks the user to pick one of <paramref name="options"/> over the page on top; the one tapped, or null when
    /// they tap <paramref name="cancel"/> or dismiss it.
    /// </summary>
    Task<string?> ChooseAsync(string title, string cancel, params string[] options);
}
