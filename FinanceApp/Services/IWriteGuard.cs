namespace FinanceApp.Services;

/// <summary>
/// The one place where a failed write is handled: services let their exceptions through, and ViewModels run every
/// write the user triggers through this guard.
/// </summary>
public interface IWriteGuard
{
    /// <summary>
    /// Runs <paramref name="write"/>. If it throws, logs the error, tells the user it wasn't saved and returns false.
    /// </summary>
    Task<bool> TryAsync(Func<Task> write);
}
