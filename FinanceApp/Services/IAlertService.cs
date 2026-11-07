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
}
