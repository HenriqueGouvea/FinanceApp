using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;

namespace FinanceApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Native dialogs (e.g. the Picker's "Cancel" button) are localized by the Android locale, not by .NET's culture.
        protected override void AttachBaseContext(Context? @base)
        {
            if (@base?.Resources?.Configuration is not { } current)
            {
                base.AttachBaseContext(@base);
                return;
            }

            var config = new Configuration(current);
            config.SetLocale(Java.Util.Locale.ForLanguageTag(AppCulture.Default.Name));
            base.AttachBaseContext(@base.CreateConfigurationContext(config));
        }
    }
}
