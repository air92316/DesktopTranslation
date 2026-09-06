using GTranslate.Translators;

namespace DesktopTranslation.Services;

/// <summary>Minimal translation contract used by the Google engine's fallback chain (testable without network).</summary>
internal interface IFallbackTranslator
{
    string Name { get; }
    bool SupportsLanguage(string languageCode);
    Task<FallbackTranslation> TranslateAsync(string text, string targetLanguage);
}

internal readonly record struct FallbackTranslation(string Translation, string? SourceLanguage);

/// <summary>Adapts a GTranslate <see cref="ITranslator"/> (Google / Google2 / Microsoft / Bing / Yandex) to the chain contract.</summary>
internal sealed class GTranslateFallbackAdapter : IFallbackTranslator
{
    private readonly ITranslator _inner;

    public GTranslateFallbackAdapter(ITranslator inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string Name => _inner.Name;

    public bool SupportsLanguage(string languageCode) => _inner.IsLanguageSupported(languageCode);

    public async Task<FallbackTranslation> TranslateAsync(string text, string targetLanguage)
    {
        var result = await _inner.TranslateAsync(text, targetLanguage);
        return new FallbackTranslation(result.Translation, result.SourceLanguage?.ISO6391);
    }
}
