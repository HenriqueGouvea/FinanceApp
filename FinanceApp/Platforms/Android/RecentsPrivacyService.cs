using Android.Views;

namespace FinanceApp.Services;

public class RecentsPrivacyService : IRecentsPrivacyService
{
    public void SetHidden(bool hidden)
    {
        if (Platform.CurrentActivity is not { } activity)
            return;

        // From Android 13 only the recent apps thumbnail is hidden. FLAG_SECURE, the fallback, also blocks
        // screenshots, which the E2E tests take.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            activity.SetRecentsScreenshotEnabled(!hidden);
        else if (hidden)
            activity.Window?.AddFlags(WindowManagerFlags.Secure);
        else
            activity.Window?.ClearFlags(WindowManagerFlags.Secure);
    }
}
