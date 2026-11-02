using FinanceApp.Services;

namespace FinanceApp.Tests.Infrastructure;

public class InMemoryPreferences : IPreferencesService
{
    private readonly Dictionary<string, bool> _values = [];

    public bool Get(string key, bool defaultValue) => _values.GetValueOrDefault(key, defaultValue);

    public void Set(string key, bool value) => _values[key] = value;
}
