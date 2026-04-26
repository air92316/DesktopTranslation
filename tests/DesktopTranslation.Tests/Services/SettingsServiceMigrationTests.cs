using System.Text.Json;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class SettingsServiceMigrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    public SettingsServiceMigrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dt_migration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new SettingsService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Migration_v1_2_13_To_v1_2_14_CopiesApiKeyToClaudeKey_WhenProviderIsClaude()
    {
        WriteLegacyJson(provider: "claude", apiKey: "claude-key-13");

        var loaded = _service.Load();

        Assert.Equal("claude-key-13", loaded.ApiKey);
        Assert.Equal("claude-key-13", loaded.ClaudeApiKey);
        Assert.Equal("", loaded.OpenAiApiKey);
        Assert.Equal("", loaded.GeminiApiKey);
    }

    [Fact]
    public void Migration_v1_2_13_To_v1_2_14_CopiesApiKeyToOpenAiKey_WhenProviderIsOpenAi()
    {
        WriteLegacyJson(provider: "openai", apiKey: "openai-key-13");

        var loaded = _service.Load();

        Assert.Equal("openai-key-13", loaded.ApiKey);
        Assert.Equal("openai-key-13", loaded.OpenAiApiKey);
        Assert.Equal("", loaded.ClaudeApiKey);
        Assert.Equal("", loaded.GeminiApiKey);
    }

    [Fact]
    public void Migration_NoOp_WhenAnyPerProviderKeyAlreadyPopulated()
    {
        var settings = new AppSettings
        {
            LlmProvider = "claude",
            ApiKey = "old-key",
            OpenAiApiKey = "preexisting-openai",
        };
        _service.Save(settings);

        var loaded = _service.Load();

        Assert.Equal("old-key", loaded.ApiKey);
        Assert.Equal("preexisting-openai", loaded.OpenAiApiKey);
        Assert.Equal("", loaded.ClaudeApiKey);
    }

    [Fact]
    public void GetEffectiveApiKey_PrefersPerProviderKey_OverApiKey()
    {
        var settings = new AppSettings
        {
            LlmProvider = "claude",
            ApiKey = "fallback",
            ClaudeApiKey = "preferred",
        };

        Assert.Equal("preferred", SettingsService.GetEffectiveApiKey(settings, "claude"));
    }

    [Fact]
    public void GetEffectiveApiKey_FallsBackToApiKey_WhenPerProviderIsEmpty()
    {
        var settings = new AppSettings
        {
            LlmProvider = "claude",
            ApiKey = "fallback",
            ClaudeApiKey = "",
        };

        Assert.Equal("fallback", SettingsService.GetEffectiveApiKey(settings, "claude"));
    }

    [Fact]
    public void GetEffectiveApiKey_OpenAi_ReadsFromOpenAiKey()
    {
        var settings = new AppSettings
        {
            LlmProvider = "openai",
            ApiKey = "shared",
            OpenAiApiKey = "openai-specific",
            ClaudeApiKey = "claude-specific",
        };

        Assert.Equal("openai-specific", SettingsService.GetEffectiveApiKey(settings, "openai"));
        Assert.Equal("claude-specific", SettingsService.GetEffectiveApiKey(settings, "claude"));
    }

    [Fact]
    public void GetEffectiveApiKey_UnknownProvider_FallsBackToApiKey()
    {
        var settings = new AppSettings { ApiKey = "shared" };

        Assert.Equal("shared", SettingsService.GetEffectiveApiKey(settings, "unknown"));
    }

    [Fact]
    public void Validate_ClampsTemperature_BelowZero()
    {
        _service.Save(new AppSettings { LlmTemperature = -1.0 });
        var loaded = _service.Load();
        Assert.Equal(0.0, loaded.LlmTemperature);
    }

    [Fact]
    public void Validate_ClampsTemperature_AboveTwo()
    {
        _service.Save(new AppSettings { LlmTemperature = 3.0 });
        var loaded = _service.Load();
        Assert.Equal(2.0, loaded.LlmTemperature);
    }

    [Fact]
    public void Validate_ClampsMaxTokens_BelowMin()
    {
        _service.Save(new AppSettings { LlmMaxTokens = 100 });
        var loaded = _service.Load();
        Assert.Equal(256, loaded.LlmMaxTokens);
    }

    [Fact]
    public void Validate_ClampsMaxTokens_AboveMax()
    {
        _service.Save(new AppSettings { LlmMaxTokens = 100000 });
        var loaded = _service.Load();
        Assert.Equal(8192, loaded.LlmMaxTokens);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips_NewLlmFields()
    {
        var settings = new AppSettings
        {
            LlmProvider = "openai",
            LlmModel = "gpt-5.4-nano",
            LlmBaseUrl = "https://api.example.com/v1",
            LlmTemperature = 0.7,
            LlmMaxTokens = 4096,
            OpenAiApiKey = "openai-rt",
            ClaudeApiKey = "claude-rt",
            GeminiApiKey = "gemini-rt",
        };
        _service.Save(settings);

        var loaded = _service.Load();
        Assert.Equal("openai", loaded.LlmProvider);
        Assert.Equal("gpt-5.4-nano", loaded.LlmModel);
        Assert.Equal("https://api.example.com/v1", loaded.LlmBaseUrl);
        Assert.Equal(0.7, loaded.LlmTemperature);
        Assert.Equal(4096, loaded.LlmMaxTokens);
        Assert.Equal("openai-rt", loaded.OpenAiApiKey);
        Assert.Equal("claude-rt", loaded.ClaudeApiKey);
        Assert.Equal("gemini-rt", loaded.GeminiApiKey);
    }

    private void WriteLegacyJson(string provider, string apiKey)
    {
        var legacy = new
        {
            windowX = 100,
            windowY = 200,
            windowWidth = 720,
            windowHeight = 400,
            alwaysOnTop = true,
            engine = "google",
            llmProvider = provider,
            apiKey = apiKey,
            autoStart = false,
            doubleTapInterval = 400,
            ttsSpeed = 1.0,
            theme = "system",
            autoUpdateEnabled = true,
            updateCheckIntervalHours = 24,
            skippedVersion = "",
            lastUpdateCheck = "0001-01-01T00:00:00",
        };
        var json = JsonSerializer.Serialize(legacy, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), json);
    }
}
