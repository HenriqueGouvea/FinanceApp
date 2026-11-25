namespace FinanceApp.Models;

/// <summary>
/// Which row of the setup wizard created an entry, so running the wizard again opens with it filled in and saving
/// updates the entry instead of adding another. Linked by id, since the entry can be renamed afterwards.
/// </summary>
public class SetupLink
{
    public int Id { get; set; }

    /// <summary>
    /// The wizard row: <see cref="SetupRows.Salary"/>, <see cref="SetupRows.Expense"/> + the expense's name, ...
    /// </summary>
    public string Row { get; set; } = string.Empty;

    /// <summary>
    /// Ties the entries of one home or car together (its financing and IPTU, or its IPVA and licensing); empty for the
    /// other rows.
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// The home's or car's name ("Casa", "Apartamento da praia"), empty for the other rows.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    public int FinancialEntryId { get; set; }
}

/// <summary>
/// The values of <see cref="SetupLink.Row"/>.
/// </summary>
public static class SetupRows
{
    public const string Salary = "salary";
    public const string Advance = "advance";
    public const string Purchase = "purchase";
    public const string Activity = "activity";
    public const string HomeFinancing = "home-financing";
    public const string HomeIptu = "home-iptu";
    public const string CarIpva = "car-ipva";
    public const string CarLicensing = "car-licensing";

    /// <summary>
    /// A ticked expense of the list ("Aluguel", "Plano de saúde"...), keyed by its name in the wizard.
    /// </summary>
    public static string Expense(string name) => $"expense:{name}";
}

/// <summary>
/// An entry the setup wizard created before, with the wizard row that created it (read when the wizard runs again).
/// </summary>
public record PreviousSetupEntry(SetupLink Link, FinancialEntry Entry);
