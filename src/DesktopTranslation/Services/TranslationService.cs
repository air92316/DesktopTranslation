using System.Diagnostics;
using DesktopTranslation.Models;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace DesktopTranslation.Services;

public class TranslationService
{
    private readonly Dictionary<string, ITranslationEngine> _engines = new();
    private readonly ResiliencePipeline _retryPipeline;

    public string CurrentEngineName { get; private set; } = "google";

    public TranslationService()
    {
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    public void RegisterEngine(string key, ITranslationEngine engine)
    {
        _engines[key] = engine;
    }

    public void SetEngine(string key)
    {
        if (_engines.TryGetValue(key, out _))
            CurrentEngineName = key;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult("", "unknown", false, "Input text is empty");

        if (!_engines.TryGetValue(CurrentEngineName, out var engine))
            return new TranslationResult("", "unknown", false, "No engine configured");

        try
        {
            return await _retryPipeline.ExecuteAsync(
                async token => await engine.TranslateAsync(text, targetLanguage, token),
                ct);
        }
        catch (TimeoutRejectedException ex)
        {
            Debug.WriteLine($"Translation pipeline timeout: {ex}");
            return new TranslationResult(
                "", "unknown", false,
                "Translation timed out.",
                ErrorKind.Timeout);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Translation pipeline error: {ex}");
            return new TranslationResult("", "unknown", false, "Translation failed. Please try again.");
        }
    }
}
