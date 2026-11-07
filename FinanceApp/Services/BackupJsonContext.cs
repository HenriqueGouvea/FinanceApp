using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceApp.Models;

namespace FinanceApp.Services;

// Source-generated so serialization keeps working once Release builds trim unused reflection metadata.
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DataBackup))]
internal partial class BackupJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Writes accents as they are ("Itaú", not "Itaú"), so the file stays readable. It is never embedded in HTML.
    /// Built on first use: the generated Default isn't initialized yet while this class's own static fields are.
    /// </summary>
    public static BackupJsonContext Readable => field ??= new(new JsonSerializerOptions(Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}
