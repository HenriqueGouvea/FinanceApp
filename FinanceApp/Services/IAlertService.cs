namespace FinanceApp.Services;

public interface IAlertService
{
    /// <summary>
    /// Shows a message with a single "OK" button over the page on top.
    /// </summary>
    Task ShowAsync(string title, string message);
}
