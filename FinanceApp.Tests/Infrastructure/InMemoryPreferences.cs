using FinanceApp.Services;

namespace FinanceApp.Tests.Infrastructure;

public class InMemoryPreferences : IPreferencesService
{
    private readonly Dictionary<string, object> _values = [];

    public bool Get(string key, bool defaultValue) => _values.TryGetValue(key, out var value) ? (bool)value : defaultValue;

    public void Set(string key, bool value) => _values[key] = value;

    public string Get(string key, string defaultValue) => _values.TryGetValue(key, out var value) ? (string)value : defaultValue;

    public void Set(string key, string value) => _values[key] = value;

    public void Clear() => _values.Clear();
}
