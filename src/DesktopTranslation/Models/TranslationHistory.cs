namespace DesktopTranslation.Models;

public record TranslationHistoryEntry(
    string SourceText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string Engine,
    DateTime Timestamp)
{
    public DateTime TimestampLocal => Timestamp.Kind == DateTimeKind.Utc
        ? Timestamp.ToLocalTime()
        : Timestamp;

    // Condensed timestamp for history meta: same-day shows HH:mm; other days add MM/dd.
    // Re-evaluated per ListBox rebind (RefreshHistoryList), which suffices for practical use.
    public string TimestampDisplay
    {
        get
        {
            var local = TimestampLocal;
            return local.Date == DateTime.Today
                ? local.ToString("HH:mm")
                : local.ToString("MM/dd HH:mm");
        }
    }

    // Display-friendly engine name (title-case / all-caps for acronyms).
    public string EngineDisplay => Engine switch
    {
        "google" => "Google",
        "llm" => "LLM",
        null or "" => "",
        _ => char.ToUpperInvariant(Engine[0]) + Engine[1..]
    };
}
