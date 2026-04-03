namespace DesktopTranslation.Models;

public record UpdateInfo(
    string Version,
    string DownloadUrl,
    long FileSizeBytes,
    string ReleaseNotes,
    DateTime PublishedAt);
