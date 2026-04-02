using System.Text.Json;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Integration;

/// <summary>
/// Integration tests for SettingsService with DPAPI encryption round-trip,
/// value clamping, and invalid value reset behavior.
/// </summary>
public class SettingsIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    public SettingsIntegrationTests()
    {
        (_service, _tempDir) = TestHelper.CreateTempSettingsService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void SaveAndLoad_ApiKey_RoundTrips()
    {
        var settings = new AppSettings { ApiKey = "sk-test-12345-abcdef" };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal("sk-test-12345-abcdef", loaded.ApiKey);
    }

    [Fact]
    public void SavedApiKey_IsNotPlaintext_InJsonFile()
    {
        var apiKey = "sk-test-12345-abcdef";
        var settings = new AppSettings { ApiKey = apiKey };
        _service.Save(settings);

        // Read raw JSON to verify encryption
        var rawJson = TestHelper.ReadRawSettingsJson(_tempDir);

        // The raw JSON should NOT contain the plaintext API key
        Assert.DoesNotContain(apiKey, rawJson);

        // The file should still be valid JSON
        var doc = JsonDocument.Parse(rawJson);
        var apiKeyProperty = doc.RootElement.GetProperty("apiKey").GetString();

        // The stored value should be a base64-encoded DPAPI blob, not the original key
        Assert.NotNull(apiKeyProperty);
        Assert.NotEmpty(apiKeyProperty);
        Assert.NotEqual(apiKey, apiKeyProperty);
    }

    [Fact]
    public void SaveAndLoad_EmptyApiKey_RoundTrips()
    {
        var settings = new AppSettings { ApiKey = "" };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal("", loaded.ApiKey);
    }

    [Fact]
    public void Save_NegativeWindowWidth_ClampedToMinimum()
    {
        var settings = new AppSettings { WindowWidth = -100 };
        _service.Save(settings);

        var loaded = _service.Load();

        // Minimum is 200 per the Validate method
        Assert.Equal(200, loaded.WindowWidth);
    }

    [Fact]
    public void Save_ExcessiveWindowWidth_ClampedToMaximum()
    {
        var settings = new AppSettings { WindowWidth = 99999 };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(3840, loaded.WindowWidth);
    }

    [Fact]
    public void Save_NegativeWindowHeight_ClampedToMinimum()
    {
        var settings = new AppSettings { WindowHeight = -50 };
        _service.Save(settings);

        var loaded = _service.Load();

        // Minimum is 150
        Assert.Equal(150, loaded.WindowHeight);
    }

    [Fact]
    public void Save_InvalidEngine_ResetToDefault()
    {
        var settings = new AppSettings { Engine = "nonexistent_engine" };
        _service.Save(settings);

        var loaded = _service.Load();

        // Default engine is "google"
        Assert.Equal("google", loaded.Engine);
    }

    [Fact]
    public void Save_InvalidTheme_ResetToDefault()
    {
        var settings = new AppSettings { Theme = "rainbow" };
        _service.Save(settings);

        var loaded = _service.Load();

        // Default theme is "system"
        Assert.Equal("system", loaded.Theme);
    }

    [Fact]
    public void Save_InvalidLlmProvider_ResetToDefault()
    {
        var settings = new AppSettings { LlmProvider = "invalid_provider" };
        _service.Save(settings);

        var loaded = _service.Load();

        // Default provider is "claude"
        Assert.Equal("claude", loaded.LlmProvider);
    }

    [Fact]
    public void Save_DoubleTapIntervalZero_ClampedToMinimum()
    {
        var settings = new AppSettings { DoubleTapInterval = 0 };
        _service.Save(settings);

        var loaded = _service.Load();

        // Minimum is 100
        Assert.Equal(100, loaded.DoubleTapInterval);
    }

    [Fact]
    public void Save_DoubleTapIntervalExcessive_ClampedToMaximum()
    {
        var settings = new AppSettings { DoubleTapInterval = 9999 };
        _service.Save(settings);

        var loaded = _service.Load();

        // Maximum is 1000
        Assert.Equal(1000, loaded.DoubleTapInterval);
    }

    [Fact]
    public void Save_TtsSpeedBelowMinimum_ClampedToMinimum()
    {
        var settings = new AppSettings { TtsSpeed = 0.1 };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(0.5, loaded.TtsSpeed);
    }

    [Fact]
    public void Save_TtsSpeedAboveMaximum_ClampedToMaximum()
    {
        var settings = new AppSettings { TtsSpeed = 10.0 };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(3.0, loaded.TtsSpeed);
    }

    [Fact]
    public void Save_MultipleFieldsClamped_AllCorrected()
    {
        var settings = new AppSettings
        {
            WindowWidth = -100,
            WindowHeight = -50,
            DoubleTapInterval = 0,
            Engine = "fake",
            Theme = "neon",
            TtsSpeed = 0.0
        };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(200, loaded.WindowWidth);
        Assert.Equal(150, loaded.WindowHeight);
        Assert.Equal(100, loaded.DoubleTapInterval);
        Assert.Equal("google", loaded.Engine);
        Assert.Equal("system", loaded.Theme);
        Assert.Equal(0.5, loaded.TtsSpeed);
    }

    [Fact]
    public void SaveAndLoad_SpecialCharactersInApiKey_RoundTrips()
    {
        var apiKey = "sk_test!@#$%^&*()_+-=[]{}|;':\",./<>?";
        var settings = new AppSettings { ApiKey = apiKey };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(apiKey, loaded.ApiKey);
    }

    [Fact]
    public void SaveAndLoad_UnicodeApiKey_RoundTrips()
    {
        var apiKey = "sk-密鑰-テスト-key-🔑";
        var settings = new AppSettings { ApiKey = apiKey };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(apiKey, loaded.ApiKey);
    }

    [Fact]
    public void SaveAndLoad_LongApiKey_RoundTrips()
    {
        var apiKey = new string('x', 500);
        var settings = new AppSettings { ApiKey = apiKey };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(apiKey, loaded.ApiKey);
    }

    [Fact]
    public void Save_ValidSettings_PreservedExactly()
    {
        var settings = new AppSettings
        {
            WindowWidth = 800,
            WindowHeight = 600,
            WindowX = 100,
            WindowY = 200,
            AlwaysOnTop = false,
            Engine = "llm",
            LlmProvider = "openai",
            ApiKey = "test-key",
            AutoStart = true,
            DoubleTapInterval = 500,
            TtsSpeed = 1.5,
            Theme = "dark"
        };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal(800, loaded.WindowWidth);
        Assert.Equal(600, loaded.WindowHeight);
        Assert.Equal(100, loaded.WindowX);
        Assert.Equal(200, loaded.WindowY);
        Assert.False(loaded.AlwaysOnTop);
        Assert.Equal("llm", loaded.Engine);
        Assert.Equal("openai", loaded.LlmProvider);
        Assert.Equal("test-key", loaded.ApiKey);
        Assert.True(loaded.AutoStart);
        Assert.Equal(500, loaded.DoubleTapInterval);
        Assert.Equal(1.5, loaded.TtsSpeed);
        Assert.Equal("dark", loaded.Theme);
    }
}
