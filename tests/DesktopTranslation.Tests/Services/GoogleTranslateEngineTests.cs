using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class GoogleTranslateEngineTests
{
    private sealed class FakeFallbackTranslator : IFallbackTranslator
    {
        private readonly Func<string, string, Task<FallbackTranslation>> _translate;
        private readonly Func<string, bool> _supports;

        public string Name { get; }
        public int CallCount { get; private set; }

        public FakeFallbackTranslator(
            string name,
            Func<string, string, Task<FallbackTranslation>> translate,
            Func<string, bool>? supports = null)
        {
            Name = name;
            _translate = translate;
            _supports = supports ?? (_ => true);
        }

        public bool SupportsLanguage(string languageCode) => _supports(languageCode);

        public Task<FallbackTranslation> TranslateAsync(string text, string targetLanguage)
        {
            CallCount++;
            return _translate(text, targetLanguage);
        }
    }

    private static FakeFallbackTranslator Succeeding(string name, string translation) =>
        new(name, (_, _) => Task.FromResult(new FallbackTranslation(translation, "en")));

    private static FakeFallbackTranslator Failing(string name, Exception error) =>
        new(name, (_, _) => Task.FromException<FallbackTranslation>(error));

    private static HttpRequestException RateLimited() =>
        new("Too Many Requests", null, (HttpStatusCode)429);

    private static HttpRequestException NetworkDown() =>
        new("No such host", new SocketException((int)SocketError.HostNotFound));

    [Fact]
    public async Task FirstTranslatorSucceeds_ReturnsItsTranslationWithoutTouchingOthers()
    {
        var google = Succeeding("Google", "你好");
        var bing = Succeeding("Bing", "哈囉");
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { google, bing });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.True(result.IsSuccess);
        Assert.Equal("你好", result.TranslatedText);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Equal(1, google.CallCount);
        Assert.Equal(0, bing.CallCount);
    }

    [Fact]
    public async Task FirstTranslatorRateLimited_FallsBackToNext()
    {
        var google = Failing("Google", RateLimited());
        var bing = Succeeding("Bing", "哈囉");
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { google, bing });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.True(result.IsSuccess);
        Assert.Equal("哈囉", result.TranslatedText);
        Assert.Equal(1, google.CallCount);
        Assert.Equal(1, bing.CallCount);
    }

    [Fact]
    public async Task AllTranslatorsRateLimited_ReturnsRateLimitError()
    {
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[]
        {
            Failing("Google", RateLimited()),
            Failing("Bing", RateLimited()),
        });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.RateLimit, result.ErrorKind);
    }

    [Fact]
    public async Task AllTranslatorsNetworkDown_ReturnsNetworkError()
    {
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[]
        {
            Failing("Google", NetworkDown()),
            Failing("Bing", NetworkDown()),
        });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Network, result.ErrorKind);
    }

    [Fact]
    public async Task MixedRateLimitAndOtherFailures_ReportsRateLimit()
    {
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[]
        {
            Failing("Google", RateLimited()),
            Failing("Bing", new InvalidOperationException("parse error")),
        });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.RateLimit, result.ErrorKind);
    }

    [Fact]
    public async Task UnsupportedLanguage_TranslatorIsSkipped()
    {
        var yandex = new FakeFallbackTranslator(
            "Yandex",
            (_, _) => Task.FromResult(new FallbackTranslation("should not be used", "en")),
            supports: _ => false);
        var bing = Succeeding("Bing", "哈囉");
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { yandex, bing });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.True(result.IsSuccess);
        Assert.Equal("哈囉", result.TranslatedText);
        Assert.Equal(0, yandex.CallCount);
    }

    [Fact]
    public async Task NoTranslatorSupportsLanguage_ReturnsUnknownError()
    {
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[]
        {
            new FakeFallbackTranslator("Google",
                (_, _) => Task.FromResult(new FallbackTranslation("x", "en")),
                supports: _ => false),
        });

        var result = await engine.TranslateAsync("hello", "xx");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unknown, result.ErrorKind);
    }

    [Fact]
    public async Task CancelledBetweenHops_StopsChainAndThrows()
    {
        using var cts = new CancellationTokenSource();
        var google = new FakeFallbackTranslator("Google", (_, _) =>
        {
            cts.Cancel(); // user typed something new while Google was in flight
            return Task.FromException<FallbackTranslation>(RateLimited());
        });
        var bing = Succeeding("Bing", "哈囉");
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { google, bing });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.TranslateAsync("hello", "zh-TW", cts.Token));

        Assert.Equal(0, bing.CallCount);
    }

    [Fact]
    public async Task EmptyText_ReturnsErrorWithoutCallingChain()
    {
        var google = Succeeding("Google", "x");
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { google });

        var result = await engine.TranslateAsync("   ", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, google.CallCount);
    }

    [Fact]
    public async Task MissingSourceLanguage_FallsBackToUnknown()
    {
        var google = new FakeFallbackTranslator("Google",
            (_, _) => Task.FromResult(new FallbackTranslation("你好", null)));
        var engine = new GoogleTranslateEngine(new IFallbackTranslator[] { google });

        var result = await engine.TranslateAsync("hello", "zh-TW");

        Assert.Equal("unknown", result.DetectedSourceLanguage);
    }

    [Fact]
    public void DefaultConstructor_BuildsFiveServiceChain()
    {
        var engine = new GoogleTranslateEngine();

        var names = engine.ChainServiceNames;
        Assert.Equal(5, names.Count);
        Assert.Contains("Google", names[0]);
        Assert.Contains("Google", names[1]);
        Assert.Contains("Microsoft", names[2]);
        Assert.Contains("Bing", names[3]);
        Assert.Contains("Yandex", names[4]);
    }
}
