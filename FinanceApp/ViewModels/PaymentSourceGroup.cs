using FinanceApp.Models;

namespace FinanceApp.ViewModels;

public class PaymentSourceGroup(PaymentSourceType type, string title, IEnumerable<PaymentSource> sources)
    : List<PaymentSource>(sources)
{
    public PaymentSourceType Type { get; } = type;
    public string Title { get; } = title;
}
