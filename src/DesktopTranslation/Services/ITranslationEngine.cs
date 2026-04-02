using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public interface ITranslationEngine
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken ct = default);
}
