namespace FinanceApp.Services;

public class UserProfileService : IUserProfileService
{
    private const string NameKey = "ProfileName";
    private const string PhotoFileKey = "ProfilePhotoFile";

    private readonly IPreferencesService _preferences;
    private readonly IFileSystem _fileSystem;

    public UserProfileService(IPreferencesService preferences, IFileSystem fileSystem)
    {
        _preferences = preferences;
        _fileSystem = fileSystem;
    }

    public string Name => _preferences.Get(NameKey, string.Empty);

    public string? PhotoPath
    {
        get
        {
            var fileName = _preferences.Get(PhotoFileKey, string.Empty);
            if (fileName.Length == 0)
                return null;

            var path = Path.Combine(_fileSystem.AppDataDirectory, fileName);
            return File.Exists(path) ? path : null;
        }
    }

    public async Task SaveAsync(string name, string? photoPath)
    {
        _preferences.Set(NameKey, name.Trim());

        var currentPath = PhotoPath;
        if (photoPath == currentPath)
            return;

        // Every photo gets a new file name: Android's image cache would otherwise keep showing the old one.
        string newFileName = string.Empty;
        if (photoPath is not null)
        {
            newFileName = $"profile-{Guid.NewGuid():N}{Path.GetExtension(photoPath)}";
            await using var source = File.OpenRead(photoPath);
            await using var target = File.Create(Path.Combine(_fileSystem.AppDataDirectory, newFileName));
            await source.CopyToAsync(target);
        }

        _preferences.Set(PhotoFileKey, newFileName);
        if (currentPath is not null)
            File.Delete(currentPath);
    }
}
