using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Integration;

/// <summary>
/// Provides common setup methods for integration tests.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Creates a TranslationService with the given engines registered and the first one set as active.
    /// </summary>
    public static TranslationService CreateTranslationService(
        params (string Key, ITranslationEngine Engine)[] engines)
    {
        var service = new TranslationService();
        foreach (var (key, engine) in engines)
            service.RegisterEngine(key, engine);

        if (engines.Length > 0)
            service.SetEngine(engines[0].Key);

        return service;
    }

    /// <summary>
    /// Creates a SettingsService backed by a unique temp directory.
    /// Returns the service and the temp directory path (caller should clean up).
    /// </summary>
    public static (SettingsService Service, string TempDir) CreateTempSettingsService()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"dt_integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return (new SettingsService(tempDir), tempDir);
    }

    /// <summary>
    /// Creates a standard FakeTranslationEngine preconfigured for Chinese-English translation.
    /// </summary>
    public static FakeTranslationEngine CreateChineseEnglishEngine(string name = "fake")
    {
        return new FakeTranslationEngine(name)
            .WithTranslation("zh-TW", "你好世界")
            .WithTranslation("en", "Hello world");
    }

    /// <summary>
    /// Creates a TranslationHistoryEntry with sensible defaults.
    /// </summary>
    public static TranslationHistoryEntry CreateHistoryEntry(
        string sourceText = "hello",
        string translatedText = "你好",
        string sourceLanguage = "en",
        string targetLanguage = "zh-TW",
        string engine = "google",
        DateTime? timestamp = null)
    {
        return new TranslationHistoryEntry(
            sourceText, translatedText, sourceLanguage, targetLanguage,
            engine, timestamp ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Reads raw JSON content from a settings file in the given directory.
    /// </summary>
    public static string ReadRawSettingsJson(string directory)
    {
        var path = Path.Combine(directory, "settings.json");
        return File.ReadAllText(path);
    }
}
