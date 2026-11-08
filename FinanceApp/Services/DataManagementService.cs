using System.Globalization;
using System.Text;
using System.Text.Json;
using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class DataManagementService : IDataManagementService
{
    private const string ExportFolder = "export";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IDatabaseContext _context;
    private readonly ICategoryService _categoryService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IAppLockService _appLockService;
    private readonly IPreferencesService _preferences;
    private readonly IFileSystem _fileSystem;
    private readonly TimeProvider _timeProvider;

    public DataManagementService(
        IDatabaseContext context,
        ICategoryService categoryService,
        IPaymentSourceService paymentSourceService,
        IUserProfileService profileService,
        IAppLockService appLockService,
        IPreferencesService preferences,
        IFileSystem fileSystem,
        TimeProvider timeProvider)
    {
        _context = context;
        _categoryService = categoryService;
        _paymentSourceService = paymentSourceService;
        _profileService = profileService;
        _appLockService = appLockService;
        _preferences = preferences;
        _fileSystem = fileSystem;
        _timeProvider = timeProvider;
    }

    public async Task DeleteAllAsync()
    {
        var db = await _context.GetConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            DeleteAllRows(conn);
            conn.InsertAll(DatabaseContext.CreateDefaultCategories(), runInTransaction: false);
        });
        ClearCaches();

        await _profileService.SaveAsync(string.Empty, null);
        _appLockService.Disable();
        // Also forgets the setup wizard was done, so it shows again as on a new install.
        _preferences.Clear();
    }

    public async Task<IReadOnlyList<string>> ExportAsync()
    {
        var db = await _context.GetConnectionAsync();
        var now = _timeProvider.GetLocalNow();
        var backup = new DataBackup
        {
            Format = DataBackup.FormatName,
            Version = DataBackup.CurrentVersion,
            ExportedAt = now,
            ProfileName = _profileService.Name,
            Categories = await db.Table<FinancialCategory>().ToListAsync(),
            PaymentSources = await db.Table<PaymentSource>().ToListAsync(),
            Entries = await db.Table<FinancialEntry>().ToListAsync(),
            MonthlyEntries = await db.Table<FinancialMonthlyEntry>().ToListAsync(),
            Payments = await db.Table<FinancialPayment>().ToListAsync()
        };
        if (_profileService.PhotoPath is { } photoPath)
        {
            backup.ProfilePhoto = Convert.ToBase64String(await File.ReadAllBytesAsync(photoPath));
            backup.ProfilePhotoExtension = Path.GetExtension(photoPath);
        }

        // Only the latest export is kept: the previous ones were already shared.
        var folder = Path.Combine(_fileSystem.CacheDirectory, ExportFolder);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);

        var date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var backupPath = Path.Combine(folder, $"mensal-backup-{date}.json");
        await using (var file = File.Create(backupPath))
            await JsonSerializer.SerializeAsync(file, backup, BackupJsonContext.Readable.DataBackup);

        var csvPath = Path.Combine(folder, $"mensal-lancamentos-{date}.csv");
        // The BOM makes Excel read the file as UTF-8 instead of the system code page.
        await File.WriteAllTextAsync(csvPath, BuildEntriesCsv(backup), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        return [backupPath, csvPath];
    }

    public async Task ImportAsync(Stream backupStream)
    {
        var backup = await ReadBackupAsync(backupStream);
        var photo = DecodePhoto(backup);

        var db = await _context.GetConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            DeleteAllRows(conn);
            // InsertOrReplace writes the ids too, so the links between rows stay as they were.
            foreach (var row in backup.Categories) conn.InsertOrReplace(row);
            foreach (var row in backup.PaymentSources) conn.InsertOrReplace(row);
            foreach (var row in backup.Entries) conn.InsertOrReplace(row);
            foreach (var row in backup.MonthlyEntries) conn.InsertOrReplace(row);
            foreach (var row in backup.Payments) conn.InsertOrReplace(row);
        });
        ClearCaches();

        if (photo is null)
        {
            await _profileService.SaveAsync(backup.ProfileName, null);
            return;
        }

        var photoPath = Path.Combine(_fileSystem.CacheDirectory, $"imported-photo-{Guid.NewGuid():N}{backup.ProfilePhotoExtension}");
        await File.WriteAllBytesAsync(photoPath, photo);
        try
        {
            await _profileService.SaveAsync(backup.ProfileName, photoPath);
        }
        finally
        {
            File.Delete(photoPath);
        }
    }

    private void ClearCaches()
    {
        _categoryService.ClearCache();
        _paymentSourceService.ClearCache();
    }

    private static void DeleteAllRows(SQLite.SQLiteConnection conn)
    {
        conn.DeleteAll<FinancialPayment>();
        conn.DeleteAll<FinancialMonthlyEntry>();
        conn.DeleteAll<FinancialEntry>();
        conn.DeleteAll<PaymentSource>();
        conn.DeleteAll<FinancialCategory>();
    }

    private static async Task<DataBackup> ReadBackupAsync(Stream stream)
    {
        DataBackup? backup;
        try
        {
            backup = await JsonSerializer.DeserializeAsync(stream, BackupJsonContext.Readable.DataBackup);
        }
        catch (JsonException ex)
        {
            throw new InvalidBackupException("The file isn't valid JSON.", ex);
        }

        if (backup is null || backup.Format != DataBackup.FormatName || backup.Version < 1)
            throw new InvalidBackupException("The file isn't a Mensal backup.");
        if (backup.Version > DataBackup.CurrentVersion)
            throw new InvalidBackupException($"The backup's version {backup.Version} is newer than this app's.") { IsFromNewerVersion = true };

        return backup;
    }

    private static byte[]? DecodePhoto(DataBackup backup)
    {
        if (string.IsNullOrEmpty(backup.ProfilePhoto))
            return null;

        try
        {
            return Convert.FromBase64String(backup.ProfilePhoto);
        }
        catch (FormatException ex)
        {
            throw new InvalidBackupException("The profile photo isn't valid base64.", ex);
        }
    }

    // Semicolons and decimal commas, as spreadsheets set to Portuguese (Brazil) expect.
    private static string BuildEntriesCsv(DataBackup backup)
    {
        var categories = backup.Categories.ToDictionary(c => c.Id, c => c.Name);
        var sources = backup.PaymentSources.ToDictionary(s => s.Id, s => s.Name);
        var csv = new StringBuilder();
        csv.AppendLine("Tipo;Descrição;Valor;Recorrência;Início;Fim;Parcelas;Vencimento;Categoria;Conta ou cartão;Valor estimado;Pagamento parcial");

        foreach (var entry in backup.Entries.OrderBy(e => e.EntryType).ThenBy(e => e.StartDate).ThenBy(e => e.Description))
        {
            string[] fields =
            [
                entry.EntryType == FinancialEntryType.Income ? "Receita" : "Despesa",
                Text(entry.Description),
                entry.Amount.ToString("0.00", PtBr),
                RecurrenceLabel(entry.Recurrence),
                entry.StartDate.ToString("MM/yyyy", PtBr),
                entry.EndDate?.ToString("MM/yyyy", PtBr) ?? string.Empty,
                entry.TotalInstallments?.ToString(PtBr) ?? string.Empty,
                entry.DueDay?.ToString(PtBr) ?? string.Empty,
                Text(entry.CategoryId is { } categoryId ? categories.GetValueOrDefault(categoryId) : null),
                Text(entry.PaymentSourceId is { } sourceId ? sources.GetValueOrDefault(sourceId) : null),
                entry.IsEstimated ? "Sim" : "Não",
                entry.AllowsPartialPayment ? "Sim" : "Não"
            ];
            csv.AppendLine(string.Join(';', fields));
        }

        return csv.ToString();
    }

    private static string RecurrenceLabel(RecurrenceType recurrence) => recurrence switch
    {
        RecurrenceType.Recurrent => "Todo mês",
        RecurrenceType.Installments => "Parcelada",
        RecurrenceType.Annual => "Todo ano",
        _ => "Única"
    };

    // Quotes the text when needed, and keeps a spreadsheet from running it as a formula (e.g. "=HYPERLINK(...)").
    private static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;

        return value.IndexOfAny([';', '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
