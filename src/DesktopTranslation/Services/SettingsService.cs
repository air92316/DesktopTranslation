using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DesktopTranslation.Helpers;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> ValidEngines = ["google", "llm"];
    private static readonly HashSet<string> ValidThemes = ["system", "light", "dark"];
    private static readonly HashSet<string> ValidProviders = ["claude", "openai", "gemini"];

    private readonly string _filePath;
    private readonly object _lock = new();

    public SettingsService(string? directory = null)
    {
        var dir = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopTranslation");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new AppSettings();

                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                               ?? new AppSettings();

                settings = settings with
                {
                    ApiKey = DecryptOrPassthrough(settings.ApiKey),
                    OpenAiApiKey = DecryptOrPassthrough(settings.OpenAiApiKey),
                    ClaudeApiKey = DecryptOrPassthrough(settings.ClaudeApiKey),
                    GeminiApiKey = DecryptOrPassthrough(settings.GeminiApiKey),
                };

                settings = Validate(settings);
                settings = MigrateLegacyApiKey(settings);
                return settings;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Settings JSON parse error: {ex.Message}");
                return new AppSettings();
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Settings file I/O error: {ex.Message}");
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        lock (_lock)
        {
            var validated = Validate(settings);
            var toSave = validated with
            {
                ApiKey = DataProtectionHelper.Protect(validated.ApiKey),
                OpenAiApiKey = DataProtectionHelper.Protect(validated.OpenAiApiKey),
                ClaudeApiKey = DataProtectionHelper.Protect(validated.ClaudeApiKey),
                GeminiApiKey = DataProtectionHelper.Protect(validated.GeminiApiKey),
            };

            var json = JsonSerializer.Serialize(toSave, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    public static string GetEffectiveApiKey(AppSettings settings, string provider)
    {
        var perProviderKey = provider switch
        {
            "claude" => settings.ClaudeApiKey,
            "openai" => settings.OpenAiApiKey,
            "gemini" => settings.GeminiApiKey,
            _ => "",
        };
        return string.IsNullOrEmpty(perProviderKey) ? settings.ApiKey : perProviderKey;
    }

    private static string DecryptOrPassthrough(string stored)
    {
        if (string.IsNullOrEmpty(stored))
            return "";

        var decrypted = DataProtectionHelper.Unprotect(stored);
        if (!string.IsNullOrEmpty(decrypted))
            return decrypted;

        return stored;
    }

    private static AppSettings MigrateLegacyApiKey(AppSettings s)
    {
        if (!string.IsNullOrEmpty(s.OpenAiApiKey)
            || !string.IsNullOrEmpty(s.ClaudeApiKey)
            || !string.IsNullOrEmpty(s.GeminiApiKey))
        {
            return s;
        }

        if (string.IsNullOrEmpty(s.ApiKey))
            return s;

        return s.LlmProvider switch
        {
            "claude" => s with { ClaudeApiKey = s.ApiKey },
            "openai" => s with { OpenAiApiKey = s.ApiKey },
            "gemini" => s with { GeminiApiKey = s.ApiKey },
            _ => s,
        };
    }

    private static AppSettings Validate(AppSettings s)
    {
        var defaults = new AppSettings();
        return s with
        {
            WindowWidth = Math.Clamp(s.WindowWidth, 200, 3840),
            WindowHeight = Math.Clamp(s.WindowHeight, 150, 2160),
            WindowX = Math.Clamp(s.WindowX, -3840, 7680),
            WindowY = Math.Clamp(s.WindowY, -2160, 4320),
            DoubleTapInterval = Math.Clamp(s.DoubleTapInterval, 100, 1000),
            TtsSpeed = Math.Clamp(s.TtsSpeed, 0.5, 3.0),
            Engine = ValidEngines.Contains(s.Engine) ? s.Engine : defaults.Engine,
            Theme = ValidThemes.Contains(s.Theme) ? s.Theme : defaults.Theme,
            LlmProvider = ValidProviders.Contains(s.LlmProvider) ? s.LlmProvider : defaults.LlmProvider,
            UpdateCheckIntervalHours = Math.Clamp(s.UpdateCheckIntervalHours, 1, 168),
            LlmTemperature = Math.Clamp(s.LlmTemperature, 0.0, 2.0),
            LlmMaxTokens = Math.Clamp(s.LlmMaxTokens, 256, 8192),
        };
    }
}
