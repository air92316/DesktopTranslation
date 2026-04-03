using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// Returns <c>null</c> if the current version is up-to-date.
    /// Throws on network failure after 3 retry attempts.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken ct = default)
    {
        const int maxRetries = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
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

                if (!IsValidDownloadUrl(asset.BrowserDownloadUrl))
                    return null;

                return new UpdateInfo(
                    Version: $"{remoteVersion.Major}.{remoteVersion.Minor}.{remoteVersion.Build}",
                    DownloadUrl: asset.BrowserDownloadUrl,
                    FileSizeBytes: asset.Size,
                    ReleaseNotes: release.Body ?? "",
                    PublishedAt: release.PublishedAt);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or TaskCanceledException or JsonException
                && !ct.IsCancellationRequested
                && attempt < maxRetries)
            {
                lastException = ex;
                Debug.WriteLine($"[UPDATE] Check attempt {attempt}/{maxRetries} failed: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Final attempt failed — rethrow for caller to handle
                Debug.WriteLine($"[UPDATE] All {maxRetries} attempts failed: {ex.Message}");
                throw;
            }
        }

        throw lastException!;
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

        try
        {
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

            // Validate downloaded file size
            var fileInfo = new FileInfo(tempPath);
            if (info.FileSizeBytes > 0 && fileInfo.Length != info.FileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Downloaded file size ({fileInfo.Length}) does not match expected ({info.FileSizeBytes}).");
            }

            return tempPath;
        }
        catch
        {
            // Clean up partial download on any failure
            try { File.Delete(tempPath); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Launches the Inno Setup installer in silent mode and shuts down the application.
    /// </summary>
    public void LaunchInstallerAndExit(string installerPath)
    {
        var fullPath = Path.GetFullPath(installerPath);
        var expectedDir = Path.GetFullPath(Path.GetTempPath());

        if (!fullPath.StartsWith(expectedDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Installer path is outside the temp directory.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Installer file not found.", fullPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
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
        if (string.IsNullOrWhiteSpace(remote) || string.IsNullOrWhiteSpace(current))
            return false;

        var remoteTag = remote.TrimStart('v', 'V');
        var currentTag = current.TrimStart('v', 'V');

        if (!System.Version.TryParse(remoteTag, out var remoteVersion))
            return false;
        if (!System.Version.TryParse(currentTag, out var currentVersion))
            return false;

        return remoteVersion > currentVersion;
    }

    internal static bool IsValidDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;
        var host = uri.Host.ToLowerInvariant();
        return host == "github.com"
            || host.EndsWith(".github.com")
            || host.EndsWith(".githubusercontent.com");
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
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
