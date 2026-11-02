using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Views;

namespace FinanceApp;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IAppLockService _appLockService;
    private bool _isLocked;

    public App(AppShell shell, IAppLockService appLockService)
    {
        InitializeComponent();
        _shell = shell;
        _appLockService = appLockService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);

        _shell.Loaded += async (_, _) =>
        {
            if (await _appLockService.IsPinEnabledAsync())
                await LockAsync();
        };
        window.Stopped += (_, _) => _appLockService.OnAppBackgrounded();
        window.Resumed += async (_, _) =>
        {
            if (await _appLockService.ShouldLockOnResumeAsync())
                await LockAsync();
        };

        return window;
    }

    private async Task LockAsync()
    {
        if (_isLocked)
            return;

        var page = _shell.Handler?.MauiContext?.Services.GetService<PinPage>()
            ?? throw new InvalidOperationException("PinPage not found.");

        _isLocked = true;
        page.Prepare(PinPurpose.Unlock, async () =>
        {
            await _shell.Navigation.PopModalAsync();
            _isLocked = false;
        });
        await _shell.Navigation.PushModalAsync(page, animated: false);
    }
}
