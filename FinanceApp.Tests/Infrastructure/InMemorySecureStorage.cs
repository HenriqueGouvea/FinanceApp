using FinanceApp.Services;

namespace FinanceApp.Tests.Infrastructure;

/// <summary>
/// Stands in for the device's secure storage, which needs a platform keystore.
/// </summary>
public class InMemorySecureStorage : ISecureStorageService
{
    private readonly Dictionary<string, string> _values = [];

    public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public void Remove(string key) => _values.Remove(key);
}
