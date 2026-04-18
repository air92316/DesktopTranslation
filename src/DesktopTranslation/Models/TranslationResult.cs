namespace DesktopTranslation.Models;

public enum ErrorKind
{
    None,
    Network,
    ApiKey,
    RateLimit,
    Timeout,
    Unknown
}

public record TranslationResult(
    string TranslatedText,
    string DetectedSourceLanguage,
    bool IsSuccess,
    string? ErrorMessage = null,
    ErrorKind ErrorKind = ErrorKind.None);
