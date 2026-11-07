using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Views;

namespace FinanceApp;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IAppLockService _appLockService;
    private readonly ISetupService _setupService;
    private readonly IRecentsPrivacyService _recentsPrivacy;
    private bool _isLocked;

    public App(AppShell shell, IAppLockService appLockService, ISetupService setupService, IRecentsPrivacyService recentsPrivacy)
    {
        InitializeComponent();
        // The Mensal palette is designed for a light background only.
        UserAppTheme = AppTheme.Light;
        _shell = shell;
        _appLockService = appLockService;
        _setupService = setupService;
        _recentsPrivacy = recentsPrivacy;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);

        // A first run has no PIN yet, so the setup wizard and the lock screen never show up together.
        _shell.Loaded += async (_, _) =>
        {
            _recentsPrivacy.SetHidden(await _appLockService.ShouldHideInRecentsAsync());

            if (await _setupService.ShouldShowAsync())
                await _shell.ShowSetupAsync(isRerun: false);
            else if (await _appLockService.IsPinEnabledAsync())
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
