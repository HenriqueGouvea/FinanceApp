using System.Diagnostics;
using SQLite;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Reads and writes the app's SQLite database straight on the device through "adb run-as",
/// so it only works for debuggable (Debug) builds.
/// </summary>
public sealed class DeviceDatabase
{
    private const string TempDevicePath = "/data/local/tmp/financeapp-e2e-seed.db3";
    private static readonly string DevicePath = $"files/{E2ESettings.DatabaseFileName}";

    public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
    {
        var snapshotPath = NewSnapshotPath();
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

    public byte[] Pull() => PullDatabaseAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Writes a copy of <paramref name="database"/>, changed by <paramref name="change"/>, as the app's database.
    /// The app must be stopped, or its open connection would overwrite it.
    /// </summary>
    public void Push(byte[] database, Action<SQLiteConnection> change)
    {
        var snapshotPath = NewSnapshotPath();
        File.WriteAllBytes(snapshotPath, database);
        try
        {
            using (var connection = new SQLiteConnection(snapshotPath))
                change(connection);

            // run-as can't receive a binary stream intact, so the file goes through the shell's temp folder.
            RunAdb("push", snapshotPath, TempDevicePath);
            RunAdb("shell", $"run-as {E2ESettings.AppPackage} sh -c 'mkdir -p files && cp {TempDevicePath} {DevicePath}'; rm {TempDevicePath}");
        }
        finally
        {
            File.Delete(snapshotPath);
        }
    }

    private static string NewSnapshotPath() =>
        Path.Combine(Path.GetTempPath(), $"financeapp-e2e-{Guid.NewGuid():N}.db3");

    private static async Task<byte[]> PullDatabaseAsync()
    {
        using var process = StartAdb(["exec-out", "run-as", E2ESettings.AppPackage, "cat", DevicePath]);
        using var output = new MemoryStream();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardOutput.BaseStream.CopyToAsync(output);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || output.Length == 0)
            throw new InvalidOperationException($"Pulling the database failed (exit {process.ExitCode}): {await errorTask}");

        return output.ToArray();
    }

    private static void RunAdb(params string[] args)
    {
        using var process = StartAdb(args);
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"adb {string.Join(' ', args)} failed (exit {process.ExitCode}): {outputTask.Result}{errorTask.Result}");
    }

    private static Process StartAdb(IEnumerable<string> args)
    {
        var startInfo = new ProcessStartInfo(E2ESettings.AdbPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in new[] { "-s", E2ESettings.DeviceName }.Concat(args))
            startInfo.ArgumentList.Add(arg);

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start adb.");
    }
}
