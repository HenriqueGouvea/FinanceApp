using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class PinViewModel : ObservableObject
{
    private enum Step { EnterCurrent, EnterNew, ConfirmNew }

    private readonly IAppLockService _appLockService;
    private readonly IBiometricService _biometricService;
    private Func<Task>? _onCompleted;
    private Step _step;
    private string _newPin = string.Empty;

    [ObservableProperty] PinPurpose purpose;
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string errorMessage = string.Empty;
    [ObservableProperty] bool canUseBiometrics;

    /// <summary>
    /// True while too many wrong PINs keep the keypad locked. The page calls <see cref="RefreshLockout"/> every second
    /// meanwhile, to count down and unlock it.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddDigitCommand))]
    bool isLockedOut;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Dots))]
    string pin = string.Empty;

    /// <summary>
    /// One filled dot per typed digit, e.g. "● ● ○ ○".
    /// </summary>
    public string Dots => string.Join(' ', Enumerable.Range(0, AppLockService.PinLength).Select(i => i < Pin.Length ? '●' : '○'));

    public bool CanCancel => Purpose != PinPurpose.Unlock;

    public PinViewModel(IAppLockService appLockService, IBiometricService biometricService)
    {
        _appLockService = appLockService;
        _biometricService = biometricService;
    }

    partial void OnPurposeChanged(PinPurpose value) => OnPropertyChanged(nameof(CanCancel));

    public void Prepare(PinPurpose purpose)
    {
        Purpose = purpose;
        CanUseBiometrics = purpose == PinPurpose.Unlock && _appLockService.IsBiometricEnabled && _biometricService.IsAvailable();
        ErrorMessage = string.Empty;
        GoTo(purpose == PinPurpose.Create ? Step.EnterNew : Step.EnterCurrent);
        if (_step == Step.EnterCurrent)
            RefreshLockout();
    }

    public void RefreshLockout()
    {
        var remaining = _appLockService.GetRemainingLockout();
        var wasLockedOut = IsLockedOut;
        IsLockedOut = remaining > TimeSpan.Zero;

        if (IsLockedOut)
            ErrorMessage = $"Muitas tentativas. Tente de novo em {FormatWait(remaining)}";
        else if (wasLockedOut)
            ErrorMessage = string.Empty;
    }

    private static string FormatWait(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return seconds < 60 ? $"{seconds} s" : $"{(int)Math.Ceiling(seconds / 60.0)} min";
    }

    public void SetCompletedCallback(Func<Task> onCompleted)
    {
        _onCompleted = onCompleted;
    }

    private bool CanAddDigit() => !IsLockedOut;

    [RelayCommand(CanExecute = nameof(CanAddDigit))]
    async Task AddDigit(string digit)
    {
        if (IsLockedOut || Pin.Length >= AppLockService.PinLength)
            return;

        ErrorMessage = string.Empty;
        Pin += digit;

        if (Pin.Length == AppLockService.PinLength)
            await SubmitAsync();
    }

    [RelayCommand]
    void DeleteDigit()
    {
        if (Pin.Length > 0)
            Pin = Pin[..^1];
    }

    [RelayCommand]
    async Task UnlockWithBiometrics()
    {
        if (CanUseBiometrics && await _biometricService.AuthenticateAsync("Desbloquear o Mensal", "Usar PIN"))
            await CompleteAsync();
    }

    private async Task SubmitAsync()
    {
        var entered = Pin;
        switch (_step)
        {
            case Step.EnterCurrent:
                if (!await _appLockService.VerifyPinAsync(entered))
                {
                    Fail("PIN incorreto. Tente novamente.", Step.EnterCurrent);
                    RefreshLockout();
                    return;
                }

                if (Purpose == PinPurpose.Change)
                {
                    GoTo(Step.EnterNew);
                    return;
                }

                if (Purpose == PinPurpose.Disable)
                    _appLockService.Disable();

                await CompleteAsync();
                break;

            case Step.EnterNew:
                _newPin = entered;
                GoTo(Step.ConfirmNew);
                break;

            case Step.ConfirmNew:
                if (entered != _newPin)
                {
                    Fail("Os PINs não conferem. Crie o PIN novamente.", Step.EnterNew);
                    return;
                }

                await _appLockService.SetPinAsync(entered);
                await CompleteAsync();
                break;
        }
    }

    private void Fail(string message, Step retryStep)
    {
        GoTo(retryStep);
        ErrorMessage = message;
    }

    private void GoTo(Step step)
    {
        _step = step;
        Pin = string.Empty;
        Title = step switch
        {
            Step.EnterCurrent when Purpose == PinPurpose.Unlock => "Digite seu PIN",
            Step.EnterCurrent => "Digite o PIN atual",
            Step.EnterNew when Purpose == PinPurpose.Change => "Digite o novo PIN",
            Step.EnterNew => $"Crie um PIN de {AppLockService.PinLength} dígitos",
            _ => "Confirme o PIN"
        };
    }

    private async Task CompleteAsync()
    {
        if (_onCompleted is not null)
            await _onCompleted();
    }
}
