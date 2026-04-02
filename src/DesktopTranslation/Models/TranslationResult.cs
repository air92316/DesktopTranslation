namespace DesktopTranslation.Models;

public record TranslationResult(
    string TranslatedText,
    string DetectedSourceLanguage,
    bool IsSuccess,
    string? ErrorMessage = null);
