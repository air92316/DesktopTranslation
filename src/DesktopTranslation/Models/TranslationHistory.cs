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
}
