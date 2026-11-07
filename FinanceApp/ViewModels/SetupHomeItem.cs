namespace FinanceApp.ViewModels;

/// <summary>
/// A home the user owns, added in the setup wizard: its monthly financing (when there is one) and its IPTU, the
/// city's yearly property tax, paid at once or in installments from <paramref name="IptuStartMonth"/>.
/// </summary>
public record SetupHomeItem(
    string Name,
    decimal Financing,
    int FinancingDueDay,
    decimal Iptu,
    int IptuStartMonth,
    int IptuInstallments)
{
    /// <summary>
    /// "Financiamento · IPTU todo ano, fevereiro a novembro (10x)".
    /// </summary>
    public string Detail
    {
        get
        {
            List<string> parts = [];
            if (Financing > 0)
                parts.Add("Financiamento");
            if (Iptu > 0)
                parts.Add($"IPTU {SetupViewModel.DescribeAnnual(IptuStartMonth, IptuInstallments).ToLowerInvariant()}");
            return string.Join(" · ", parts);
        }
    }
}
