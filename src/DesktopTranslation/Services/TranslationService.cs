using System.Diagnostics;
using DesktopTranslation.Models;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace DesktopTranslation.Services;

public class TranslationService
{
    /// <summary>Upper bound for one translation including any retry. Long LLM outputs need more than the old 10 s per attempt.</summary>
    private static readonly TimeSpan DefaultTotalTimeout = TimeSpan.FromSeconds(20);

    private readonly Dictionary<string, ITranslationEngine> _engines = new();
    private readonly ResiliencePipeline<TranslationResult> _pipeline;
    private readonly TranslationCache _cache;

    public string CurrentEngineName { get; private set; } = "google";

    public TranslationService() : this(DefaultTotalTimeout)
    {
    }

    internal TranslationService(TimeSpan totalTimeout, TranslationCache? cache = null)
    {
        _cache = cache ?? new TranslationCache();

        // Order matters in Polly v8: the first strategy added is the outermost.
        // Timeout outside → one overall deadline; retry inside → at most one extra attempt,
        // and only for transient failures. Rate limits (429) and bad keys (401) are never retried
        // because a retry would only make the throttling worse.
        _pipeline = new ResiliencePipelineBuilder<TranslationResult>()
            .AddTimeout(totalTimeout)
            .AddRetry(new RetryStrategyOptions<TranslationResult>
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = args => new ValueTask<bool>(IsTransient(args.Outcome)),
            })
            .Build();
    }

    private static bool IsTransient(Outcome<TranslationResult> outcome)
    {
        if (outcome.Exception is { } ex)
            return ex is not OperationCanceledException;

        return outcome.Result is { IsSuccess: false, ErrorKind: ErrorKind.Network or ErrorKind.Timeout };
    }

    public void RegisterEngine(string key, ITranslationEngine engine)
    {
        _engines[key] = engine;
        // A re-registered engine may be configured differently (new model, new key): drop stale results.
        _cache.Clear();
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

        var engineName = CurrentEngineName;
        if (!_engines.TryGetValue(engineName, out var engine))
            return new TranslationResult("", "unknown", false, "No engine configured");

        if (_cache.TryGet(engineName, targetLanguage, text, out var cached))
            return cached;

        try
        {
            var result = await _pipeline.ExecuteAsync(
                async token => await engine.TranslateAsync(text, targetLanguage, token),
                ct);

            _cache.Set(engineName, targetLanguage, text, result);
            return result;
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
