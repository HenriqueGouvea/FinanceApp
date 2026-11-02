namespace FinanceApp.Services;

/// <summary>
/// The user's name and photo, kept on this device only (there is no account yet).
/// </summary>
public interface IUserProfileService
{
    string Name { get; }

    /// <summary>
    /// Full path of the profile photo in the app's data, or null when there is none.
    /// </summary>
    string? PhotoPath { get; }

    /// <summary>
    /// Saves the name and the photo. <paramref name="photoPath"/> is copied into the app's data unless it already is
    /// the current photo; null removes the photo.
    /// </summary>
    Task SaveAsync(string name, string? photoPath);
}
