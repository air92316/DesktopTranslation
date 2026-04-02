using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Integration;

/// <summary>
/// A configurable fake implementation of ITranslationEngine for integration testing.
/// Supports setting return values per target language, tracking call history,
/// and simulating failures.
/// </summary>
public sealed class FakeTranslationEngine : ITranslationEngine
{
    private readonly Dictionary<string, string> _translations = new();
    private readonly List<(string Text, string TargetLanguage)> _callHistory = new();
    private Exception? _exceptionToThrow;

    public string Name { get; }

    public IReadOnlyList<(string Text, string TargetLanguage)> CallHistory => _callHistory.AsReadOnly();

    public FakeTranslationEngine(string name = "fake")
    {
        Name = name;
    }

    public FakeTranslationEngine WithTranslation(string targetLanguage, string translatedText)
    {
        _translations[targetLanguage] = translatedText;
        return this;
    }

    public FakeTranslationEngine WithDefaultTranslation(string translatedText)
    {
        _translations["*"] = translatedText;
        return this;
    }

    public FakeTranslationEngine WithException(Exception exception)
    {
        _exceptionToThrow = exception;
        return this;
    }

    public Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        _callHistory.Add((text, targetLanguage));

        if (_exceptionToThrow is not null)
            throw _exceptionToThrow;

        if (_translations.TryGetValue(targetLanguage, out var translated))
            return Task.FromResult(new TranslationResult(translated, "auto", true));

        if (_translations.TryGetValue("*", out var defaultTranslated))
            return Task.FromResult(new TranslationResult(defaultTranslated, "auto", true));

        return Task.FromResult(new TranslationResult(
            $"[translated:{targetLanguage}]{text}", "auto", true));
    }
}
