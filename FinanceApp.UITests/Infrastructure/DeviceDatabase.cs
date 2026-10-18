using System.Diagnostics;
using SQLite;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Reads the app's SQLite database straight from the device through "adb exec-out run-as",
/// so it only works for debuggable (Debug) builds.
/// </summary>
public sealed class DeviceDatabase
{
    public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
    {
        var snapshotPath = Path.Combine(Path.GetTempPath(), $"financeapp-e2e-{Guid.NewGuid():N}.db3");
        await File.WriteAllBytesAsync(snapshotPath, await PullDatabaseAsync());

        var connection = new SQLiteAsyncConnection(snapshotPath, SQLiteOpenFlags.ReadOnly);
        try
        {
            return await connection.QueryAsync<T>(sql, args);
        }
        finally
        {
            await connection.CloseAsync();
            File.Delete(snapshotPath);
        }
    }

    private static async Task<byte[]> PullDatabaseAsync()
    {
        var startInfo = new ProcessStartInfo(E2ESettings.AdbPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in new[] { "-s", E2ESettings.DeviceName, "exec-out", "run-as", E2ESettings.AppPackage, "cat", $"files/{E2ESettings.DatabaseFileName}" })
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start adb.");
        using var output = new MemoryStream();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardOutput.BaseStream.CopyToAsync(output);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || output.Length == 0)
            throw new InvalidOperationException($"Pulling the database failed (exit {process.ExitCode}): {await errorTask}");

        return output.ToArray();
    }
}
