using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class TranslationCacheTests
{
    private static TranslationResult Success(string text) => new(text, "en", true);

    [Fact]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
        var cache = new TranslationCache();

        var hit = cache.TryGet("google", "zh-TW", "hello", out _);

        Assert.False(hit);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsStoredResult()
    {
        var cache = new TranslationCache();
        cache.Set("google", "zh-TW", "hello", Success("你好"));

        var hit = cache.TryGet("google", "zh-TW", "hello", out var result);

        Assert.True(hit);
        Assert.Equal("你好", result.TranslatedText);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Set_FailedResult_IsIgnored()
    {
        var cache = new TranslationCache();
        cache.Set("google", "zh-TW", "hello", new TranslationResult("", "unknown", false, "boom", ErrorKind.Network));

        Assert.False(cache.TryGet("google", "zh-TW", "hello", out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Key_IncludesEngineAndTargetLanguage()
    {
        var cache = new TranslationCache();
        cache.Set("google", "zh-TW", "hello", Success("你好"));

        Assert.False(cache.TryGet("llm", "zh-TW", "hello", out _));
        Assert.False(cache.TryGet("google", "ja", "hello", out _));
        Assert.True(cache.TryGet("google", "zh-TW", "hello", out _));
    }

    [Fact]
    public void Set_BeyondCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new TranslationCache(capacity: 2);
        cache.Set("google", "zh-TW", "one", Success("1"));
        cache.Set("google", "zh-TW", "two", Success("2"));

        // Touch "one" so "two" becomes the least recently used entry.
        Assert.True(cache.TryGet("google", "zh-TW", "one", out _));

        cache.Set("google", "zh-TW", "three", Success("3"));

        Assert.Equal(2, cache.Count);
        Assert.True(cache.TryGet("google", "zh-TW", "one", out _));
        Assert.False(cache.TryGet("google", "zh-TW", "two", out _));
        Assert.True(cache.TryGet("google", "zh-TW", "three", out _));
    }

    [Fact]
    public void Set_SameKeyTwice_OverwritesWithoutGrowing()
    {
        var cache = new TranslationCache();
        cache.Set("google", "zh-TW", "hello", Success("第一次"));
        cache.Set("google", "zh-TW", "hello", Success("第二次"));

        Assert.True(cache.TryGet("google", "zh-TW", "hello", out var result));
        Assert.Equal("第二次", result.TranslatedText);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var cache = new TranslationCache();
        cache.Set("google", "zh-TW", "hello", Success("你好"));

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("google", "zh-TW", "hello", out _));
    }

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationCache(0));
    }
}
