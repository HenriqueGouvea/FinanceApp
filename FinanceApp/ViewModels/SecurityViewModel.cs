using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class SecurityViewModel : ObservableObject
{
    private readonly IAppLockService _appLockService;
    private readonly IBiometricService _biometricService;
    private readonly IRecentsPrivacyService _recentsPrivacy;

    [ObservableProperty] bool isPinEnabled;
    [ObservableProperty] bool isBiometricEnabled;
    [ObservableProperty] bool isHideInRecentsEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BiometricHint))]
    bool isBiometricAvailable;

    public string BiometricHint => IsBiometricAvailable
        ? "Use a digital ou o rosto em vez do PIN"
        : "Nenhuma biometria cadastrada neste aparelho";

    public SecurityViewModel(IAppLockService appLockService, IBiometricService biometricService, IRecentsPrivacyService recentsPrivacy)
    {
        _appLockService = appLockService;
        _biometricService = biometricService;
        _recentsPrivacy = recentsPrivacy;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsPinEnabled = await _appLockService.IsPinEnabledAsync();
        IsBiometricEnabled = _appLockService.IsBiometricEnabled;
        IsBiometricAvailable = _biometricService.IsAvailable();
        IsHideInRecentsEnabled = _appLockService.IsHideInRecentsEnabled;
        // Runs again after the PIN is turned on or off.
        _recentsPrivacy.SetHidden(await _appLockService.ShouldHideInRecentsAsync());
    }

    [RelayCommand]
    void ToggleHideInRecents()
    {
        if (!IsPinEnabled)
            return;

        IsHideInRecentsEnabled = !IsHideInRecentsEnabled;
        _appLockService.SetHideInRecentsEnabled(IsHideInRecentsEnabled);
        _recentsPrivacy.SetHidden(IsHideInRecentsEnabled);
    }

    /// <summary>
    /// Turning biometrics on asks for a fingerprint or face first, so it's only enabled once it's known to work.
    /// </summary>
    [RelayCommand]
    async Task ToggleBiometric()
    {
        if (!IsPinEnabled)
            return;

        if (IsBiometricEnabled)
        {
            _appLockService.SetBiometricEnabled(false);
            IsBiometricEnabled = false;
            return;
        }

        if (!IsBiometricAvailable || !await _biometricService.AuthenticateAsync("Ativar biometria", "Cancelar"))
            return;

        _appLockService.SetBiometricEnabled(true);
        IsBiometricEnabled = true;
    }
}
