namespace FinanceApp.Services;

public class AlertService : IAlertService
{
    public Task ShowAsync(string title, string message)
    {
        var topPage = GetTopPage();
        return topPage is null
            ? Task.CompletedTask
            : MainThread.InvokeOnMainThreadAsync(() => topPage.DisplayAlertAsync(title, message, "OK"));
    }

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var topPage = GetTopPage();
        return topPage is null
            ? Task.FromResult(false)
            : MainThread.InvokeOnMainThreadAsync(() => topPage.DisplayAlertAsync(title, message, accept, cancel));
    }

    public async Task<string?> ChooseAsync(string title, string cancel, params string[] options)
    {
        var topPage = GetTopPage();
        if (topPage is null)
            return null;

        var choice = await MainThread.InvokeOnMainThreadAsync(() => topPage.DisplayActionSheetAsync(title, cancel, null, options));
        return options.Contains(choice) ? choice : null;
    }

    // Modal pages (the setup wizard, the PIN, popups) sit above the window's page.
    private static Page? GetTopPage()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.Navigation.ModalStack.LastOrDefault() ?? page;
    }
}
