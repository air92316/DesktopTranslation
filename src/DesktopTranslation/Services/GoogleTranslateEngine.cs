using System.Diagnostics;
using GTranslate.Translators;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class GoogleTranslateEngine : ITranslationEngine
{
    private readonly GoogleTranslator _translator = new();

    public string Name => "Google";

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult("", "unknown", false, "Input text is empty");

        try
        {
            var result = await _translator.TranslateAsync(text, targetLanguage);
            return new TranslationResult(
                TranslatedText: result.Translation,
                DetectedSourceLanguage: result.SourceLanguage.ISO6391 ?? "unknown",
                IsSuccess: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Google translation error: {ex}");
            return new TranslationResult(
                TranslatedText: "",
                DetectedSourceLanguage: "unknown",
                IsSuccess: false,
                ErrorMessage: "Translation failed. Please check your connection and try again.");
        }
    }
}
