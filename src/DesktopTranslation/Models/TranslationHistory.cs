namespace DesktopTranslation.Models;

public record TranslationHistoryEntry(
    string SourceText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string Engine,
    DateTime Timestamp);
