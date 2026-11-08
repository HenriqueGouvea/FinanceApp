using FinanceApp.Models;

namespace FinanceApp.Services;

public interface IPaymentSourceService
{
    /// <summary>
    /// All payment sources sorted by name. The list is cached and shared by every caller: don't change its items.
    /// </summary>
    Task<IReadOnlyList<PaymentSource>> GetAllAsync();

    /// <summary>
    /// Drops the cached list, for writes made outside this service (setup wizard, backup import, wipe).
    /// </summary>
    void ClearCache();

    Task SaveAsync(PaymentSource source);

    Task<int> CountEntriesAsync(int sourceId);

    /// <summary>
    /// Deletes the payment source and unlinks the entries that used it.
    /// </summary>
    Task DeleteAsync(int sourceId);
}
