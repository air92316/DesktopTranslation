using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Integration;

/// <summary>
/// End-to-end translation flow tests: LanguageDetector -> TranslationService -> FakeEngine.
/// These tests verify the full pipeline without GUI dependencies.
/// </summary>
public class TranslationFlowTests
{
    [Fact]
    public async Task EnglishInput_DetectsNonChinese_TranslatesToZhTW()
    {
        // Arrange
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "你好世界");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "Hello world";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("zh-TW", targetLang);
        Assert.True(result.IsSuccess);
        Assert.Equal("你好世界", result.TranslatedText);

        Assert.Single(engine.CallHistory);
        Assert.Equal("Hello world", engine.CallHistory[0].Text);
        Assert.Equal("zh-TW", engine.CallHistory[0].TargetLanguage);
    }

    [Fact]
    public async Task ChineseInput_DetectsChinese_TranslatesToEn()
    {
        // Arrange
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("en", "Hello world");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "你好世界";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("en", targetLang);
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello world", result.TranslatedText);

        Assert.Single(engine.CallHistory);
        Assert.Equal("你好世界", engine.CallHistory[0].Text);
        Assert.Equal("en", engine.CallHistory[0].TargetLanguage);
    }

    [Fact]
    public async Task JapaneseInput_DetectsNonChinese_TranslatesToZhTW()
    {
        // Japanese hiragana/katakana are NOT in CJK Unified Ideographs range,
        // so they are treated as non-CJK -> target = zh-TW
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "你好");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "こんにちは";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("zh-TW", targetLang);
        Assert.True(result.IsSuccess);
        Assert.Equal("你好", result.TranslatedText);
    }

    [Fact]
    public async Task MixedLanguage_ChineseDominant_TranslatesToEn()
    {
        // "這是test測試" -> CJK ratio > 30% -> target = en
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("en", "This is a test");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "這是test測試";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("en", targetLang);
        Assert.True(result.IsSuccess);
        Assert.Equal("This is a test", result.TranslatedText);
    }

    [Fact]
    public async Task MixedLanguage_EnglishDominant_TranslatesToZhTW()
    {
        // "Hello 你好 world" -> CJK ratio < 30% -> target = zh-TW
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "你好 你好 世界");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "Hello 你好 world";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("zh-TW", targetLang);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SwitchEngine_TranslationUsesNewEngine()
    {
        // Arrange: register two engines
        var googleEngine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "Google翻譯結果");
        var llmEngine = new FakeTranslationEngine("llm")
            .WithTranslation("zh-TW", "LLM翻譯結果");

        var service = TestHelper.CreateTranslationService(
            ("google", googleEngine), ("llm", llmEngine));

        var input = "Hello world";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act 1: translate with google (default, set by CreateTranslationService)
        var result1 = await service.TranslateAsync(input, targetLang);
        Assert.Equal("Google翻譯結果", result1.TranslatedText);
        Assert.Single(googleEngine.CallHistory);
        Assert.Empty(llmEngine.CallHistory);

        // Act 2: switch to llm and translate again
        service.SetEngine("llm");
        var result2 = await service.TranslateAsync(input, targetLang);
        Assert.Equal("LLM翻譯結果", result2.TranslatedText);
        Assert.Single(llmEngine.CallHistory);

        // Google engine should still have only 1 call
        Assert.Single(googleEngine.CallHistory);
    }

    [Fact]
    public async Task EmptyInput_DetectsDefaultLanguage_ReturnsEmptyError()
    {
        var engine = new FakeTranslationEngine("google")
            .WithDefaultTranslation("should not be called");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert: LanguageDetector returns zh-TW for empty, but TranslateAsync catches empty
        Assert.Equal("zh-TW", targetLang);
        Assert.False(result.IsSuccess);
        Assert.Equal("Input text is empty", result.ErrorMessage);
        Assert.Empty(engine.CallHistory);
    }

    [Fact]
    public async Task EngineFailure_ReturnsGracefulError()
    {
        var engine = new FakeTranslationEngine("google")
            .WithException(new InvalidOperationException("API down"));
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "Hello world";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert: the retry pipeline catches the exception
        Assert.False(result.IsSuccess);
        Assert.Contains("Translation failed", result.ErrorMessage);
    }

    [Fact]
    public async Task JapaneseKanji_DetectsAsCjk_TranslatesToEn()
    {
        // Japanese kanji (shared with Chinese) like "日本語" uses CJK range
        // "日本語" -> 3 CJK chars out of 3 total = 100% -> target = en
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("en", "Japanese");
        var service = TestHelper.CreateTranslationService(("google", engine));

        var input = "日本語";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act
        var result = await service.TranslateAsync(input, targetLang);

        // Assert
        Assert.Equal("en", targetLang);
        Assert.True(result.IsSuccess);
        Assert.Equal("Japanese", result.TranslatedText);
    }
}
