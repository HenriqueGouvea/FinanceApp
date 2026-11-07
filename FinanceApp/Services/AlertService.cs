namespace FinanceApp.Services;

public class AlertService : IAlertService
{
    public Task ShowAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        // Modal pages (the setup wizard, the PIN, popups) sit above the window's page.
        var topPage = page?.Navigation.ModalStack.LastOrDefault() ?? page;

        return topPage is null
            ? Task.CompletedTask
            : MainThread.InvokeOnMainThreadAsync(() => topPage.DisplayAlertAsync(title, message, "OK"));
    }
}
