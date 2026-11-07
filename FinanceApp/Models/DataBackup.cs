namespace FinanceApp.Models;

/// <summary>
/// The "Exportar dados" file: every table of the database plus the profile, so "Importar backup" can rebuild them on
/// another phone. The PIN is device-only and never leaves the phone.
/// </summary>
public class DataBackup
{
    public const string FormatName = "mensal-backup";
    public const int CurrentVersion = 1;

    public string Format { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset ExportedAt { get; set; }
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// The profile photo, base64-encoded, or null when there is none.
    /// </summary>
    public string? ProfilePhoto { get; set; }

    /// <summary>
    /// The photo's file extension (e.g. ".jpg"), so it keeps its image type when restored.
    /// </summary>
    public string? ProfilePhotoExtension { get; set; }

    public List<FinancialCategory> Categories { get; set; } = [];
    public List<PaymentSource> PaymentSources { get; set; } = [];
    public List<FinancialEntry> Entries { get; set; } = [];
    public List<FinancialMonthlyEntry> MonthlyEntries { get; set; } = [];
    public List<FinancialPayment> Payments { get; set; } = [];
}
