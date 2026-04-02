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
    public void RegisterEngine_OverwritesExisting()
    {
        var service = new TranslationService();
        var engine1 = new FakeEngine(new TranslationResult("first", "en", true));
        var engine2 = new FakeEngine(new TranslationResult("second", "en", true));

        service.RegisterEngine("google", engine1);
        service.RegisterEngine("google", engine2);

        // Verify by calling translate — should use the second engine
        service.SetEngine("google");
        var result = service.TranslateAsync("test", "zh-TW").Result;
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
}
