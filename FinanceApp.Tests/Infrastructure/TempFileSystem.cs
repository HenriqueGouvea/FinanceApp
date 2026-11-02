using Microsoft.Maui.Storage;

namespace FinanceApp.Tests.Infrastructure;

/// <summary>
/// An <see cref="IFileSystem"/> whose app data directory is a fresh temp folder, deleted when the test ends.
/// </summary>
public sealed class TempFileSystem : IFileSystem, IDisposable
{
    public TempFileSystem()
    {
        AppDataDirectory = Directory.CreateTempSubdirectory("financeapp-tests-").FullName;
    }

    public string AppDataDirectory { get; }

    public string CacheDirectory => AppDataDirectory;

    /// <summary>
    /// Creates a file outside the app data directory, as the photo picker returns.
    /// </summary>
    public static string CreateExternalFile(string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, content);
        return path;
    }

    public Task<Stream> OpenAppPackageFileAsync(string filename) => throw new NotSupportedException();

    public Task<bool> AppPackageFileExistsAsync(string filename) => throw new NotSupportedException();

    public void Dispose() => Directory.Delete(AppDataDirectory, recursive: true);
}
