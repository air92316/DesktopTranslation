using System.Diagnostics;
using System.Net.Http;
using DesktopTranslation.Models;
using DesktopTranslation.Services.Llm;
using GTranslate.Translators;

namespace DesktopTranslation.Services;

/// <summary>
/// Free translation engine. Tries Google first and falls back through other free services
/// (Google's newer RPC API, Microsoft/Edge, Bing, Yandex) when a service is rate-limited or down,
/// so a 429 from translate.googleapis.com no longer surfaces as an error to the user.
/// </summary>
public class GoogleTranslateEngine : ITranslationEngine
{
    /// <summary>
    /// Per-service HTTP timeout. GTranslate calls themselves cannot be cancelled, so each hop is
    /// additionally raced against the caller's <see cref="CancellationToken"/> in <see cref="RunWithCancellation"/>
    /// so the caller's own deadline (e.g. TranslationService's total timeout) is a hard upper bound.
    /// </summary>
    private static readonly TimeSpan PerServiceTimeout = TimeSpan.FromSeconds(6);

    private readonly IReadOnlyList<IFallbackTranslator> _chain;

    public GoogleTranslateEngine() : this(CreateDefaultChain())
    {
    }

    internal GoogleTranslateEngine(IReadOnlyList<IFallbackTranslator> chain)
    {
        _chain = chain ?? throw new ArgumentNullException(nameof(chain));
    }

    public string Name => "Google";

    internal IReadOnlyList<string> ChainServiceNames => _chain.Select(t => t.Name).ToList();

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult("", "unknown", false, "Input text is empty");

        var failures = new List<Exception>();

        foreach (var translator in _chain)
        {
            // GTranslate has no cancellation support; at least don't start the next hop
            // once the caller (debounce / hide window) has moved on.
            ct.ThrowIfCancellationRequested();

            if (!translator.SupportsLanguage(targetLanguage))
                continue;

            try
            {
                var result = await RunWithCancellation(translator.TranslateAsync(text, targetLanguage), ct);
                if (failures.Count > 0)
                    Debug.WriteLine($"Translation served by {translator.Name} after {failures.Count} failed service(s)");

                return new TranslationResult(
                    TranslatedText: result.Translation,
                    DetectedSourceLanguage: result.SourceLanguage ?? "unknown",
                    IsSuccess: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                Debug.WriteLine($"{translator.Name} translation error: {ex}");
                failures.Add(ex);
            }
        }

        return new TranslationResult(
            TranslatedText: "",
            DetectedSourceLanguage: "unknown",
            IsSuccess: false,
            ErrorMessage: "Translation failed. Please check your connection and try again.",
            ErrorKind: ClassifyFailures(failures, ct));
    }

    /// <summary>
    /// Races a single fallback-service hop against the caller's cancellation token, since
    /// <see cref="IFallbackTranslator.TranslateAsync"/> itself has no cancellation support (GTranslate
    /// does not accept a <see cref="CancellationToken"/>). If <paramref name="ct"/> fires first, the
    /// abandoned hop is left to complete in the background (its exception, if any, is observed so it
    /// doesn't surface as an unobserved task exception) and this method throws immediately instead of
    /// waiting for the hop to finish.
    /// </summary>
    private static async Task<FallbackTranslation> RunWithCancellation(
        Task<FallbackTranslation> hop, CancellationToken ct)
    {
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = ct.Register(() => cancelled.TrySetResult(true));
        var finished = await Task.WhenAny(hop, cancelled.Task);
        if (finished != hop)
        {
            // Let the abandoned hop finish in the background without an unobserved exception.
            _ = hop.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            ct.ThrowIfCancellationRequested();
        }

        return await hop;
    }

    /// <summary>
    /// Collapses the per-service failures into one user-facing kind:
    /// every service unreachable → Network; any service throttled → RateLimit; otherwise the first known kind.
    /// </summary>
    private static ErrorKind ClassifyFailures(List<Exception> failures, CancellationToken ct)
    {
        if (failures.Count == 0)
            return ErrorKind.Unknown;

        var kinds = failures.Select(ex => ProviderErrorHelpers.Classify(ex, ct)).ToList();

        if (kinds.All(k => k == ErrorKind.Network))
            return ErrorKind.Network;

        if (kinds.Contains(ErrorKind.RateLimit))
            return ErrorKind.RateLimit;

        return kinds.FirstOrDefault(k => k != ErrorKind.Unknown, ErrorKind.Unknown);
    }

    private static IReadOnlyList<IFallbackTranslator> CreateDefaultChain() => new IFallbackTranslator[]
    {
        new GTranslateFallbackAdapter(new GoogleTranslator(CreateHttpClient())),
        new GTranslateFallbackAdapter(new GoogleTranslator2(CreateHttpClient())),
        new GTranslateFallbackAdapter(new MicrosoftTranslator(CreateHttpClient())),
        new GTranslateFallbackAdapter(new BingTranslator(CreateHttpClient())),
        new GTranslateFallbackAdapter(new YandexTranslator(CreateHttpClient())),
    };

    private static HttpClient CreateHttpClient() => new() { Timeout = PerServiceTimeout };
}
