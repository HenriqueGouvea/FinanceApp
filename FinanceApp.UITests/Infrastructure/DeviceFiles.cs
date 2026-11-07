namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Files on the device outside the database: what the app wrote to its private folders (read through "adb run-as",
/// so Debug builds only) and files in the shared Downloads folder, which the system file picker lists.
/// </summary>
public static class DeviceFiles
{
    private const string DownloadsFolder = "/sdcard/Download";

    /// <summary>
    /// Text of the app's private file matching <paramref name="pattern"/> (relative to its data folder, shell wildcards
    /// allowed, e.g. "cache/export/*.json").
    /// </summary>
    public static async Task<string> ReadAppFileAsync(string pattern)
    {
        using var process = DeviceDatabase.StartAdb(["exec-out", "run-as", E2ESettings.AppPackage, "sh", "-c", $"cat {pattern}"]);
        // Read as bytes: the redirected text reader would decode the UTF-8 file with the console's code page.
        using var output = new MemoryStream();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardOutput.BaseStream.CopyToAsync(output);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || output.Length == 0)
            throw new InvalidOperationException($"Reading {pattern} failed (exit {process.ExitCode}): {await errorTask}");

        return System.Text.Encoding.UTF8.GetString(output.ToArray()).TrimStart('﻿');
    }

    /// <summary>
    /// Writes a file to Downloads and has the media store index it, so the file picker lists it under "Recent".
    /// </summary>
    public static void AddDownload(string fileName, string content)
    {
        var localPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(localPath, content);
        try
        {
            DeviceDatabase.RunAdb("push", localPath, $"{DownloadsFolder}/{fileName}");
            ScanDownloads();
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    public static void DeleteDownload(string fileName)
    {
        DeviceDatabase.RunAdb("shell", $"rm -f {DownloadsFolder}/{fileName}");
        ScanDownloads();
    }

    private static void ScanDownloads() =>
        DeviceDatabase.RunAdb("shell", "content call --uri content://media --method scan_volume --arg external_primary");
}
