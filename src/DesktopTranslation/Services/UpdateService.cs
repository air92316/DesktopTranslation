using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class UpdateService
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string GitHubApiUrl =
        "https://api.github.com/repos/air92316/DesktopTranslation/releases/latest";

    private const string InstallerPrefix = "DesktopTranslation-v";
    private const string InstallerSuffix = "-Setup.exe";
    private const int TimeoutSeconds = 10;
    private const int CleanupDays = 7;

    static UpdateService()
    {
        var version = typeof(UpdateService).Assembly.GetName().Version;
        var versionStr = $"{version?.Major}.{version?.Minor}.{version?.Build}";

        HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
        HttpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DesktopTranslation", versionStr));
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version.
    /// Returns <c>null</c> if the current version is up-to-date or on network failure.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken ct = default)
    {
        try
        {
            var json = await HttpClient.GetStringAsync(GitHubApiUrl, ct);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, JsonOptions);

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                return null;

            var remoteTag = release.TagName.TrimStart('v', 'V');
            if (!System.Version.TryParse(remoteTag, out var remoteVersion))
                return null;

            var currentTag = currentVersion.TrimStart('v', 'V');
            if (!System.Version.TryParse(currentTag, out var current))
                return null;

            if (remoteVersion <= current)
                return null;

            var asset = FindInstallerAsset(release);
            if (asset is null)
                return null;

            return new UpdateInfo(
                Version: $"v{remoteVersion.Major}.{remoteVersion.Minor}.{remoteVersion.Build}",
                DownloadUrl: asset.BrowserDownloadUrl,
                FileSizeBytes: asset.Size,
                ReleaseNotes: release.Body ?? "",
                PublishedAt: release.PublishedAt);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads the installer to %TEMP% and reports progress (0.0 ~ 1.0).
    /// Returns the full path to the downloaded file.
    /// </summary>
    public async Task<string> DownloadUpdateAsync(
        UpdateInfo info,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var fileName = $"{InstallerPrefix}{info.Version}{InstallerSuffix}";
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        using var response = await HttpClient.GetAsync(
            info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? info.FileSizeBytes;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(
            tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);

        var buffer = new byte[81920];
        long bytesRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;

            if (totalBytes > 0)
                progress?.Report((double)bytesRead / totalBytes);
        }

        progress?.Report(1.0);
        return tempPath;
    }

    /// <summary>
    /// Launches the Inno Setup installer in silent mode and shuts down the application.
    /// </summary>
    public void LaunchInstallerAndExit(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true
        });

        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// Deletes installer files in %TEMP% older than 7 days.
    /// </summary>
    public void CleanupOldInstallers()
    {
        try
        {
            var tempDir = Path.GetTempPath();
            var pattern = $"{InstallerPrefix}*{InstallerSuffix}";
            var cutoff = DateTime.Now.AddDays(-CleanupDays);

            foreach (var file in Directory.EnumerateFiles(tempDir, pattern))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTime < cutoff)
                        info.Delete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to delete old installer {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Installer cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Compares two version strings and returns true if <paramref name="remote"/> is newer
    /// than <paramref name="current"/>. Returns false on parse failure.
    /// </summary>
    public static bool IsNewerVersion(string remote, string current)
    {
        var remoteTag = remote.TrimStart('v', 'V');
        var currentTag = current.TrimStart('v', 'V');

        if (!System.Version.TryParse(remoteTag, out var remoteVersion))
            return false;
        if (!System.Version.TryParse(currentTag, out var currentVersion))
            return false;

        return remoteVersion > currentVersion;
    }

    private static GitHubAsset? FindInstallerAsset(GitHubRelease release)
    {
        if (release.Assets is null)
            return null;

        return Array.Find(release.Assets, a =>
            a.Name.EndsWith(InstallerSuffix, StringComparison.OrdinalIgnoreCase) &&
            a.Name.StartsWith(InstallerPrefix, StringComparison.OrdinalIgnoreCase));
    }

    // ── GitHub API DTOs ──────────────────────────────────────────────

    private sealed class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public string? Body { get; set; }
        public DateTime PublishedAt { get; set; }
        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
        public long Size { get; set; }
    }
}
