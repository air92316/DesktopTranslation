using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Integration;

/// <summary>
/// Integration tests for HistoryService combined with TranslationService.
/// Tests that translation results can be recorded to history and that
/// history limits are enforced correctly.
/// </summary>
public class HistoryIntegrationTests
{
    [Fact]
    public async Task TranslateThenAddToHistory_RecordIsCorrect()
    {
        // Arrange
        var engine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "你好世界");
        var translationService = TestHelper.CreateTranslationService(("google", engine));
        var historyService = new HistoryService(maxEntries: 50);

        var input = "Hello world";
        var targetLang = LanguageDetector.GetTargetLanguage(input);

        // Act: translate, then add to history
        var result = await translationService.TranslateAsync(input, targetLang);

        Assert.True(result.IsSuccess);

        historyService.Add(new TranslationHistoryEntry(
            input, result.TranslatedText, "en", targetLang,
            translationService.CurrentEngineName, DateTime.UtcNow));

        // Assert
        var history = historyService.GetAll();
        Assert.Single(history);

        var entry = history[0];
        Assert.Equal("Hello world", entry.SourceText);
        Assert.Equal("你好世界", entry.TranslatedText);
        Assert.Equal("en", entry.SourceLanguage);
        Assert.Equal("zh-TW", entry.TargetLanguage);
        Assert.Equal("google", entry.Engine);
    }

    [Fact]
    public async Task MultipleTranslations_HistoryInOrder()
    {
        // Arrange
        var engine = TestHelper.CreateChineseEnglishEngine("google");
        var translationService = TestHelper.CreateTranslationService(("google", engine));
        var historyService = new HistoryService(maxEntries: 50);

        var inputs = new[]
        {
            ("Hello world", "zh-TW"),
            ("你好世界", "en"),
            ("Good morning", "zh-TW")
        };

        // Act: translate each and record to history
        foreach (var (text, _) in inputs)
        {
            var targetLang = LanguageDetector.GetTargetLanguage(text);
            var result = await translationService.TranslateAsync(text, targetLang);

            historyService.Add(new TranslationHistoryEntry(
                text, result.TranslatedText, "auto", targetLang,
                translationService.CurrentEngineName, DateTime.UtcNow));
        }

        // Assert: history maintains insertion order
        var history = historyService.GetAll();
        Assert.Equal(3, history.Count);
        Assert.Equal("Hello world", history[0].SourceText);
        Assert.Equal("你好世界", history[1].SourceText);
        Assert.Equal("Good morning", history[2].SourceText);
    }

    [Fact]
    public async Task ExceedMaxEntries_OldestRemoved()
    {
        // Arrange: limit to 50
        var engine = new FakeTranslationEngine("google")
            .WithDefaultTranslation("translated");
        var translationService = TestHelper.CreateTranslationService(("google", engine));
        var historyService = new HistoryService(maxEntries: 50);

        // Act: add 55 entries
        for (int i = 0; i < 55; i++)
        {
            var text = $"text_{i:D3}";
            var result = await translationService.TranslateAsync(text, "zh-TW");

            historyService.Add(new TranslationHistoryEntry(
                text, result.TranslatedText, "en", "zh-TW",
                translationService.CurrentEngineName, DateTime.UtcNow));
        }

        // Assert
        var history = historyService.GetAll();
        Assert.Equal(50, history.Count);

        // The first 5 entries (0-4) should have been evicted
        Assert.Equal("text_005", history[0].SourceText);
        Assert.Equal("text_054", history[49].SourceText);
    }

    [Fact]
    public async Task EngineSwitchRecordedInHistory()
    {
        // Arrange
        var googleEngine = new FakeTranslationEngine("google")
            .WithTranslation("zh-TW", "Google result");
        var llmEngine = new FakeTranslationEngine("llm")
            .WithTranslation("zh-TW", "LLM result");

        var translationService = TestHelper.CreateTranslationService(
            ("google", googleEngine), ("llm", llmEngine));
        var historyService = new HistoryService(maxEntries: 50);

        // Act 1: translate with google
        var result1 = await translationService.TranslateAsync("hello", "zh-TW");
        historyService.Add(new TranslationHistoryEntry(
            "hello", result1.TranslatedText, "en", "zh-TW",
            translationService.CurrentEngineName, DateTime.UtcNow));

        // Act 2: switch to llm and translate
        translationService.SetEngine("llm");
        var result2 = await translationService.TranslateAsync("world", "zh-TW");
        historyService.Add(new TranslationHistoryEntry(
            "world", result2.TranslatedText, "en", "zh-TW",
            translationService.CurrentEngineName, DateTime.UtcNow));

        // Assert
        var history = historyService.GetAll();
        Assert.Equal(2, history.Count);
        Assert.Equal("google", history[0].Engine);
        Assert.Equal("Google result", history[0].TranslatedText);
        Assert.Equal("llm", history[1].Engine);
        Assert.Equal("LLM result", history[1].TranslatedText);
    }

    [Fact]
    public async Task FailedTranslation_NotAddedToHistory()
    {
        // Arrange
        var engine = new FakeTranslationEngine("google")
            .WithException(new InvalidOperationException("API error"));
        var translationService = TestHelper.CreateTranslationService(("google", engine));
        var historyService = new HistoryService(maxEntries: 50);

        // Act
        var result = await translationService.TranslateAsync("hello", "zh-TW");

        // Only add successful translations to history
        if (result.IsSuccess)
        {
            historyService.Add(new TranslationHistoryEntry(
                "hello", result.TranslatedText, "en", "zh-TW",
                translationService.CurrentEngineName, DateTime.UtcNow));
        }

        // Assert: failed translation should not be in history
        Assert.False(result.IsSuccess);
        Assert.Empty(historyService.GetAll());
    }

    [Fact]
    public void ExactlyAtMax_ThenOneMore_RemovesFirst()
    {
        var historyService = new HistoryService(maxEntries: 50);

        // Fill exactly to max
        for (int i = 0; i < 50; i++)
        {
            historyService.Add(TestHelper.CreateHistoryEntry(
                sourceText: $"entry_{i:D3}"));
        }

        Assert.Equal(50, historyService.GetAll().Count);
        Assert.Equal("entry_000", historyService.GetAll()[0].SourceText);

        // Add one more
        historyService.Add(TestHelper.CreateHistoryEntry(sourceText: "entry_050"));

        Assert.Equal(50, historyService.GetAll().Count);
        Assert.Equal("entry_001", historyService.GetAll()[0].SourceText);
        Assert.Equal("entry_050", historyService.GetAll()[49].SourceText);
    }

    [Fact]
    public void ClearHistory_ThenTranslateAndAdd_WorksNormally()
    {
        var historyService = new HistoryService(maxEntries: 50);

        // Add some entries
        for (int i = 0; i < 5; i++)
        {
            historyService.Add(TestHelper.CreateHistoryEntry(sourceText: $"old_{i}"));
        }

        Assert.Equal(5, historyService.GetAll().Count);

        // Clear
        historyService.Clear();
        Assert.Empty(historyService.GetAll());

        // Add new entry after clear
        historyService.Add(TestHelper.CreateHistoryEntry(sourceText: "new_entry"));
        Assert.Single(historyService.GetAll());
        Assert.Equal("new_entry", historyService.GetAll()[0].SourceText);
    }
}
