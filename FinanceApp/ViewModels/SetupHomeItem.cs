using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A home the user owns, added in the setup wizard: its monthly financing (when there is one) and its IPTU, the
/// city's yearly property tax, paid at once or in installments from <paramref name="IptuStartMonth"/>. A financing
/// with <paramref name="FinancingTotalInstallments"/> ends on its last installment, and
/// <paramref name="FinancingCurrentInstallment"/> falls this month; without them, it goes on every month.
/// </summary>
public record SetupHomeItem(
    string Name,
    decimal Financing,
    int FinancingDueDay,
    decimal Iptu,
    int IptuStartMonth,
    int IptuInstallments,
    PaymentSource? PaymentSource = null,
    int? FinancingTotalInstallments = null,
    int? FinancingCurrentInstallment = null)
{
    public bool HasFinancingInstallments => FinancingTotalInstallments is > 0;

    /// <summary>
    /// "Financiamento 9/120 · IPTU todo ano, fevereiro a novembro (10x) · Nubank".
    /// </summary>
    public string Detail
    {
        get
        {
            List<string> parts = [];
            if (Financing > 0)
                parts.Add(HasFinancingInstallments ? $"Financiamento {FinancingCurrentInstallment}/{FinancingTotalInstallments}" : "Financiamento");
            if (Iptu > 0)
                parts.Add($"IPTU {SetupViewModel.DescribeAnnual(IptuStartMonth, IptuInstallments).ToLowerInvariant()}");
            if (PaymentSource is { } source)
                parts.Add(source.Name);
            return string.Join(" · ", parts);
        }
    }
}
