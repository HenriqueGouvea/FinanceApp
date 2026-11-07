namespace FinanceApp.Services;

public class PreferencesService : IPreferencesService
{
    public bool Get(string key, bool defaultValue) => Preferences.Default.Get(key, defaultValue);

    public void Set(string key, bool value) => Preferences.Default.Set(key, value);

    public string Get(string key, string defaultValue) => Preferences.Default.Get(key, defaultValue);

    public void Set(string key, string value) => Preferences.Default.Set(key, value);

    public void Clear() => Preferences.Default.Clear();
}
