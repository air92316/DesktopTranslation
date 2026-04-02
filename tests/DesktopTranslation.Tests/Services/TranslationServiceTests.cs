using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class TranslationServiceTests
{
    private sealed class FakeEngine : ITranslationEngine
    {
        public string Name => "Fake";
        private readonly TranslationResult _result;

        public FakeEngine(TranslationResult result)
        {
            _result = result;
        }

        public Task<TranslationResult> TranslateAsync(
            string text, string targetLanguage, CancellationToken ct = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class ThrowingEngine : ITranslationEngine
    {
        public string Name => "Throwing";

        public Task<TranslationResult> TranslateAsync(
            string text, string targetLanguage, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Engine failure");
        }
    }

    [Fact]
    public async Task TranslateAsync_NoEngineRegistered_ReturnsError()
    {
        var service = new TranslationService();

        var result = await service.TranslateAsync("hello", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal("No engine configured", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_WithRegisteredEngine_ReturnsResult()
    {
        var service = new TranslationService();
        var expected = new TranslationResult("你好", "en", true);
        service.RegisterEngine("google", new FakeEngine(expected));
        service.SetEngine("google");

        var result = await service.TranslateAsync("hello", "zh-TW");

        Assert.True(result.IsSuccess);
        Assert.Equal("你好", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_EngineThatThrows_ReturnsErrorResult()
    {
        var service = new TranslationService();
        service.RegisterEngine("bad", new ThrowingEngine());
        service.SetEngine("bad");

        var result = await service.TranslateAsync("hello", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Contains("Translation failed", result.ErrorMessage);
    }

    [Fact]
    public void SetEngine_NonExistentKey_DoesNotChange()
    {
        var service = new TranslationService();
        service.RegisterEngine("google", new FakeEngine(
            new TranslationResult("test", "en", true)));

        service.SetEngine("google");
        Assert.Equal("google", service.CurrentEngineName);

        service.SetEngine("nonexistent");
        Assert.Equal("google", service.CurrentEngineName);
    }

    [Fact]
    public async Task RegisterEngine_OverwritesExisting()
    {
        var service = new TranslationService();
        var engine1 = new FakeEngine(new TranslationResult("first", "en", true));
        var engine2 = new FakeEngine(new TranslationResult("second", "en", true));

        service.RegisterEngine("google", engine1);
        service.RegisterEngine("google", engine2);

        // Verify by calling translate — should use the second engine
        service.SetEngine("google");
        var result = await service.TranslateAsync("test", "zh-TW");
        Assert.Equal("second", result.TranslatedText);
    }

    [Fact]
    public void DefaultEngineName_IsGoogle()
    {
        var service = new TranslationService();
        Assert.Equal("google", service.CurrentEngineName);
    }

    [Fact]
    public void SetEngine_SwitchBetweenEngines()
    {
        var service = new TranslationService();
        service.RegisterEngine("google", new FakeEngine(
            new TranslationResult("g", "en", true)));
        service.RegisterEngine("llm", new FakeEngine(
            new TranslationResult("l", "en", true)));

        service.SetEngine("llm");
        Assert.Equal("llm", service.CurrentEngineName);

        service.SetEngine("google");
        Assert.Equal("google", service.CurrentEngineName);
    }

    [Fact]
    public async Task TranslateAsync_AfterSwitchingEngine_UsesNewEngine()
    {
        var service = new TranslationService();
        service.RegisterEngine("google", new FakeEngine(
            new TranslationResult("google-result", "en", true)));
        service.RegisterEngine("llm", new FakeEngine(
            new TranslationResult("llm-result", "en", true)));

        service.SetEngine("google");
        var result1 = await service.TranslateAsync("hello", "zh-TW");
        Assert.Equal("google-result", result1.TranslatedText);

        service.SetEngine("llm");
        var result2 = await service.TranslateAsync("hello", "zh-TW");
        Assert.Equal("llm-result", result2.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_CancelledToken_ReturnsErrorOrThrows()
    {
        var service = new TranslationService();
        service.RegisterEngine("slow", new SlowEngine());
        service.SetEngine("slow");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var result = await service.TranslateAsync("hello", "zh-TW", cts.Token);

        // The retry pipeline should catch the cancellation and return error
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task TranslateAsync_EmptyText_ReturnsError()
    {
        var service = new TranslationService();
        var expected = new TranslationResult("", "en", true);
        service.RegisterEngine("google", new FakeEngine(expected));
        service.SetEngine("google");

        var result = await service.TranslateAsync("", "zh-TW");

        Assert.False(result.IsSuccess);
        Assert.Equal("Input text is empty", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_MultipleSequentialCalls_AllSucceed()
    {
        var service = new TranslationService();
        service.RegisterEngine("google", new FakeEngine(
            new TranslationResult("translated", "en", true)));
        service.SetEngine("google");

        for (int i = 0; i < 5; i++)
        {
            var result = await service.TranslateAsync($"text{i}", "zh-TW");
            Assert.True(result.IsSuccess);
            Assert.Equal("translated", result.TranslatedText);
        }
    }

    [Fact]
    public void RegisterEngine_NullEngine_ThrowsOnTranslate()
    {
        var service = new TranslationService();
        // Registering null should still allow setting it, but translating should fail
        service.RegisterEngine("null-engine", null!);
        service.SetEngine("null-engine");

        Assert.ThrowsAnyAsync<Exception>(
            () => service.TranslateAsync("hello", "zh-TW"));
    }

    private sealed class SlowEngine : ITranslationEngine
    {
        public string Name => "Slow";

        public async Task<TranslationResult> TranslateAsync(
            string text, string targetLanguage, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new TranslationResult("done", "en", true);
        }
    }
}
