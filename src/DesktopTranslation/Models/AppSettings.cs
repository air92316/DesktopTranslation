namespace DesktopTranslation.Models;

public record AppSettings
{
    public double WindowX { get; init; } = 100;
    public double WindowY { get; init; } = 200;
    public double WindowWidth { get; init; } = 720;
    public double WindowHeight { get; init; } = 400;
    public bool AlwaysOnTop { get; init; } = true;
    public string Engine { get; init; } = "google";
    public string LlmProvider { get; init; } = "claude";
    public string ApiKey { get; init; } = "";
    public bool AutoStart { get; init; } = false;
    public int DoubleTapInterval { get; init; } = 400;
    public double TtsSpeed { get; init; } = 1.0;
    public string Theme { get; init; } = "system";
    public bool AutoUpdateEnabled { get; init; } = true;
    public int UpdateCheckIntervalHours { get; init; } = 24;
    public string SkippedVersion { get; init; } = "";
    public DateTime LastUpdateCheck { get; init; } = DateTime.MinValue;
}
