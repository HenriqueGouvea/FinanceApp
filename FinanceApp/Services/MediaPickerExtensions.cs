namespace FinanceApp.Services;

public static class MediaPickerExtensions
{
    /// <summary>
    /// Opens the device's photo picker for a single profile photo. Returns its path, or null when the user cancels or
    /// the picker can't be used (e.g. the permission was denied).
    /// </summary>
    public static async Task<string?> PickProfilePhotoAsync(this IMediaPicker mediaPicker)
    {
        try
        {
            var photos = await mediaPicker.PickPhotosAsync(new MediaPickerOptions { Title = "Escolha sua foto", SelectionLimit = 1 });
            return photos.FirstOrDefault()?.FullPath;
        }
        catch (Exception ex) when (ex is PermissionException or FeatureNotSupportedException)
        {
            Console.WriteLine($"Error picking the profile photo: {ex.Message}");
            return null;
        }
    }
}
