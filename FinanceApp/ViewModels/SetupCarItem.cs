using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A car added in the setup wizard with its yearly taxes: IPVA, paid at once or in installments from
/// <paramref name="IpvaStartMonth"/>, and the licensing fee ("licenciamento"), paid once a year in
/// <paramref name="LicensingMonth"/>. Both fall due by the plate's final digit on each state's calendar, so the user
/// picks the months.
/// </summary>
public record SetupCarItem(
    string Name,
    decimal Ipva,
    int IpvaStartMonth,
    int IpvaInstallments,
    decimal Licensing,
    int LicensingMonth,
    PaymentSource? PaymentSource = null)
{
    /// <summary>
    /// "IPVA todo ano, janeiro a março (3x) · Licenciamento todo ano em julho · Nubank".
    /// </summary>
    public string Detail
    {
        get
        {
            List<string> parts = [];
            if (Ipva > 0)
                parts.Add($"IPVA {SetupViewModel.DescribeAnnual(IpvaStartMonth, IpvaInstallments).ToLowerInvariant()}");
            if (Licensing > 0)
                parts.Add($"Licenciamento {SetupViewModel.DescribeAnnual(LicensingMonth, 1).ToLowerInvariant()}");
            if (PaymentSource is { } source)
                parts.Add(source.Name);
            return string.Join(" · ", parts);
        }
    }
}
