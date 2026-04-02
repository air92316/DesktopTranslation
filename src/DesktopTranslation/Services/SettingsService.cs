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
    private static readonly HashSet<string> ValidProviders = ["claude", "openai"];

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

                // Decrypt API key (DPAPI)
                var decryptedKey = DataProtectionHelper.Unprotect(settings.ApiKey);
                if (string.IsNullOrEmpty(decryptedKey) && !string.IsNullOrEmpty(settings.ApiKey))
                {
                    // Migration: stored key is plaintext (pre-encryption era)
                    // Return as-is; it will be encrypted on next Save()
                    decryptedKey = settings.ApiKey;
                }

                settings = settings with { ApiKey = decryptedKey };
                return Validate(settings);
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
            // Encrypt API key before persisting
            var encryptedKey = DataProtectionHelper.Protect(settings.ApiKey);
            var toSave = Validate(settings) with { ApiKey = encryptedKey };

            var json = JsonSerializer.Serialize(toSave, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
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
        };
    }
}
