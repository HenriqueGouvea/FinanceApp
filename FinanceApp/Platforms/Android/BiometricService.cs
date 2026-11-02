using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;

namespace FinanceApp.Services;

public class BiometricService : IBiometricService
{
    // Weak biometrics include the face unlock of most phones; the prompt only gates the UI, no key is unlocked.
    private const int Authenticators = BiometricManager.Authenticators.BiometricWeak;

    public bool IsAvailable() =>
        BiometricManager.From(Platform.AppContext).CanAuthenticate(Authenticators) == BiometricManager.BiometricSuccess;

    public Task<bool> AuthenticateAsync(string title, string cancelText)
    {
        if (Platform.CurrentActivity is not FragmentActivity activity)
            return Task.FromResult(false);

        var result = new TaskCompletionSource<bool>();
        var prompt = new BiometricPrompt(activity, ContextCompat.GetMainExecutor(activity)!, new Callback(result));
        var info = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(title)
            .SetNegativeButtonText(cancelText)
            .SetAllowedAuthenticators(Authenticators)
            .Build();

        prompt.Authenticate(info);
        return result.Task;
    }

    // OnAuthenticationFailed (a finger that didn't match) is not final: the prompt stays open for another try.
    private sealed class Callback(TaskCompletionSource<bool> result) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult authResult) =>
            result.TrySetResult(true);

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString) =>
            result.TrySetResult(false);
    }
}
