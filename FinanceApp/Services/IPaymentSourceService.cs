using FinanceApp.Models;

namespace FinanceApp.Services;

public interface IPaymentSourceService
{
    Task<IReadOnlyList<PaymentSource>> GetAllAsync();

    Task SaveAsync(PaymentSource source);

    Task<int> CountEntriesAsync(int sourceId);

    /// <summary>
    /// Deletes the payment source and unlinks the entries that used it.
    /// </summary>
    Task DeleteAsync(int sourceId);
}
