namespace FinanceApp.Services;

/// <summary>
/// The user's control over their own data: wiping it, exporting it to a file and importing it back, so they can
/// change phones without a Google backup.
/// </summary>
public interface IDataManagementService
{
    /// <summary>
    /// Deletes every entry, account and card and category (the default categories come back, as on a new install),
    /// the profile, the PIN and the settings, so the setup wizard shows again.
    /// </summary>
    Task DeleteAllAsync();

    /// <summary>
    /// Writes the backup (JSON, for "Importar backup") and the entries as a spreadsheet (CSV) to the cache folder
    /// and returns their paths, backup first.
    /// </summary>
    Task<IReadOnlyList<string>> ExportAsync();

    /// <summary>
    /// Replaces every entry, account and card and category, and the profile, with the backup's. The PIN and the
    /// settings of this phone are kept.
    /// </summary>
    /// <exception cref="InvalidBackupException">The file isn't a backup made by "Exportar dados".</exception>
    Task ImportAsync(Stream backup);
}

public class InvalidBackupException(string message, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// The backup was made by a newer version of the app, so updating the app lets it be imported.
    /// </summary>
    public bool IsFromNewerVersion { get; init; }
}
