namespace FinanceApp.Services;

public interface IPreferencesService
{
    bool Get(string key, bool defaultValue);
    void Set(string key, bool value);
    string Get(string key, string defaultValue);
    void Set(string key, string value);

    /// <summary>
    /// Removes every preference, as on a new install.
    /// </summary>
    void Clear();
}
