using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using DesktopTranslation.Models;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace DesktopTranslation.Services;

public class TranslationService
{
    /// <summary>
    /// Hard upper bound for one translation including any retry. Long LLM outputs need more than the old
    /// 10 s per attempt. This is enforced as a real deadline: GoogleTranslateEngine races each of its
    /// fallback-chain hops against this same cancellation token, so a slow underlying HTTP call can no
    /// longer stretch the total wait past this timeout.
    /// </summary>
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
        // Timeout outside → one hard overall deadline (GoogleTranslateEngine races its own per-service
        // hops against this same token, so an in-flight hop can no longer stretch this deadline);
        // retry inside → at most one extra attempt, and only for transient failures. Rate limits (429)
        // and bad keys (401) are never retried because a retry would only make the throttling worse.
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
            return IsTransientException(ex);

        return outcome.Result is { IsSuccess: false, ErrorKind: ErrorKind.Network or ErrorKind.Timeout };
    }

    /// <summary>
    /// Only known transient failures are retried: network errors, socket errors, and timeouts
    /// (including an HttpClient-internal timeout, which surfaces as a TaskCanceledException whose own
    /// token was never cancelled). A genuine bug (e.g. NullReferenceException, InvalidOperationException)
    /// or an actual caller-requested cancellation is not retried — retrying would just mask the bug or
    /// fight the caller's own cancellation.
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        if (ex is TaskCanceledException taskCanceled)
            return !taskCanceled.CancellationToken.IsCancellationRequested;

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException or TimeoutException)
                return true;
        }

        return false;
    }

    public void RegisterEngine(string key, ITranslationEngine engine)
    {
        _engines[key] = engine;
        // A re-registered engine may be configured differently (new model, new key): drop stale results.
        _cache.Clear();
    }

    /// <summary>Removes an engine (e.g. LLM after its API key was cleared). Falls back to "google" if it was active.</summary>
    public void UnregisterEngine(string key)
    {
        if (!_engines.Remove(key))
            return;

        _cache.Clear();
        if (CurrentEngineName == key)
            CurrentEngineName = _engines.ContainsKey("google") ? "google" : _engines.Keys.FirstOrDefault() ?? key;
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
