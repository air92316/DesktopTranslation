# DesktopTranslation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows desktop translation tool that triggers on double Ctrl+C, translates clipboard text via Google Translate or LLM, and displays results in a floating window.

**Architecture:** Single-layer WPF app with Services/Views/Models structure. Low-level keyboard hook detects double Ctrl+C, reads clipboard, auto-detects language (CJK Unicode ratio), dispatches to translation engine, displays in a draggable floating window with left (input) / right (output) panes.

**Tech Stack:** C# / .NET 8 / WPF / System.Text.Json / System.Speech / P/Invoke (Win32) / GTranslate / Polly / Claudia / OpenAI / H.NotifyIcon.Wpf

---

## File Structure

```
DesktopTranslation/
├── DesktopTranslation.sln
├── src/
│   └── DesktopTranslation/
│       ├── DesktopTranslation.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── Models/
│       │   ├── AppSettings.cs
│       │   ├── TranslationResult.cs
│       │   └── TranslationHistory.cs
│       ├── Services/
│       │   ├── ITranslationEngine.cs
│       │   ├── GoogleTranslateEngine.cs
│       │   ├── LlmTranslateEngine.cs
│       │   ├── TranslationService.cs
│       │   ├── HotkeyService.cs
│       │   ├── ClipboardService.cs
│       │   ├── LanguageDetector.cs
│       │   ├── TtsService.cs
│       │   ├── SettingsService.cs
│       │   ├── AutoStartService.cs
│       │   └── HistoryService.cs
│       ├── Views/
│       │   ├── TranslationWindow.xaml
│       │   ├── TranslationWindow.xaml.cs
│       │   ├── SettingsWindow.xaml
│       │   ├── SettingsWindow.xaml.cs
│       │   └── TrayIconManager.cs
│       ├── Helpers/
│       │   ├── Win32Interop.cs
│       │   └── ThemeHelper.cs
│       └── Assets/
│           └── tray-icon.ico
├── tests/
│   └── DesktopTranslation.Tests/
│       ├── DesktopTranslation.Tests.csproj
│       ├── Services/
│       │   ├── LanguageDetectorTests.cs
│       │   ├── TranslationServiceTests.cs
│       │   ├── SettingsServiceTests.cs
│       │   ├── HistoryServiceTests.cs
│       │   └── AutoStartServiceTests.cs
│       └── Models/
│           └── AppSettingsTests.cs
└── installer/
    └── setup.iss
```

---

## Task 1: Project Scaffolding & NuGet Setup

**Team:** Dev
**Files:**
- Create: `DesktopTranslation.sln`
- Create: `src/DesktopTranslation/DesktopTranslation.csproj`
- Create: `tests/DesktopTranslation.Tests/DesktopTranslation.Tests.csproj`

- [ ] **Step 1: Create solution and WPF project**

```bash
cd D:/Tool/DesktopTranslation
dotnet new sln -n DesktopTranslation
mkdir -p src/DesktopTranslation
cd src/DesktopTranslation
dotnet new wpf -n DesktopTranslation --framework net8.0-windows
cd ../..
dotnet sln add src/DesktopTranslation/DesktopTranslation.csproj
```

- [ ] **Step 2: Add NuGet packages**

```bash
cd src/DesktopTranslation
dotnet add package GTranslate --version 4.*
dotnet add package Polly --version 8.*
dotnet add package Claudia --version 3.*
dotnet add package OpenAI --version 2.*
dotnet add package H.NotifyIcon.Wpf --version 2.*
```

- [ ] **Step 3: Add System.Speech reference to csproj**

Edit `src/DesktopTranslation/DesktopTranslation.csproj` to add:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" />
</ItemGroup>
<ItemGroup>
  <Reference Include="System.Speech" />
</ItemGroup>
```

Also set `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` for P/Invoke if needed.

- [ ] **Step 4: Create test project**

```bash
mkdir -p tests/DesktopTranslation.Tests
cd tests/DesktopTranslation.Tests
dotnet new xunit -n DesktopTranslation.Tests --framework net8.0-windows
dotnet add reference ../../src/DesktopTranslation/DesktopTranslation.csproj
cd ../..
dotnet sln add tests/DesktopTranslation.Tests/DesktopTranslation.Tests.csproj
```

- [ ] **Step 5: Create directory structure**

```bash
cd src/DesktopTranslation
mkdir -p Models Services Views Helpers Assets
cd ../..
cd tests/DesktopTranslation.Tests
mkdir -p Services Models
```

- [ ] **Step 6: Verify build**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git init
git add -A
git commit -m "chore: scaffold WPF project with NuGet packages and test project"
```

---

## Task 2: Models

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Models/AppSettings.cs`
- Create: `src/DesktopTranslation/Models/TranslationResult.cs`
- Create: `src/DesktopTranslation/Models/TranslationHistory.cs`
- Test: `tests/DesktopTranslation.Tests/Models/AppSettingsTests.cs`

- [ ] **Step 1: Write AppSettings tests**

```csharp
// tests/DesktopTranslation.Tests/Models/AppSettingsTests.cs
using DesktopTranslation.Models;

namespace DesktopTranslation.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Default_Values_Are_Correct()
    {
        var settings = new AppSettings();

        Assert.Equal(720, settings.WindowWidth);
        Assert.Equal(400, settings.WindowHeight);
        Assert.True(settings.AlwaysOnTop);
        Assert.Equal("google", settings.Engine);
        Assert.Equal("claude", settings.LlmProvider);
        Assert.Equal("", settings.ApiKey);
        Assert.False(settings.AutoStart);
        Assert.Equal(400, settings.DoubleTapInterval);
        Assert.Equal(1.0, settings.TtsSpeed);
        Assert.Equal("system", settings.Theme);
    }

    [Fact]
    public void Clone_Creates_Independent_Copy()
    {
        var original = new AppSettings { Engine = "google" };
        var clone = original with { Engine = "llm" };

        Assert.Equal("google", original.Engine);
        Assert.Equal("llm", clone.Engine);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "AppSettingsTests"
```
Expected: FAIL — AppSettings not found.

- [ ] **Step 3: Implement models**

```csharp
// src/DesktopTranslation/Models/AppSettings.cs
namespace DesktopTranslation.Models;

public record AppSettings
{
    public double WindowX { get; init; } = 100;
    public double WindowY { get; init; } = 200;
    public double WindowWidth { get; init; } = 720;
    public double WindowHeight { get; init; } = 400;
    public bool AlwaysOnTop { get; init; } = true;
    public string Engine { get; init; } = "google";
    public string LlmProvider { get; init; } = "claude";
    public string ApiKey { get; init; } = "";
    public bool AutoStart { get; init; } = false;
    public int DoubleTapInterval { get; init; } = 400;
    public double TtsSpeed { get; init; } = 1.0;
    public string Theme { get; init; } = "system";
}
```

```csharp
// src/DesktopTranslation/Models/TranslationResult.cs
namespace DesktopTranslation.Models;

public record TranslationResult(
    string TranslatedText,
    string DetectedSourceLanguage,
    bool IsSuccess,
    string? ErrorMessage = null);
```

```csharp
// src/DesktopTranslation/Models/TranslationHistory.cs
namespace DesktopTranslation.Models;

public record TranslationHistoryEntry(
    string SourceText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string Engine,
    DateTime Timestamp);
```

- [ ] **Step 4: Run test — expect PASS**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "AppSettingsTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add AppSettings, TranslationResult, TranslationHistory models"
```

---

## Task 3: Win32 Interop & Helpers

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Helpers/Win32Interop.cs`
- Create: `src/DesktopTranslation/Helpers/ThemeHelper.cs`

- [ ] **Step 1: Implement Win32Interop**

```csharp
// src/DesktopTranslation/Helpers/Win32Interop.cs
using System.Runtime.InteropServices;

namespace DesktopTranslation.Helpers;

public static class Win32Interop
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int VK_CONTROL = 0x11;
    public const int VK_C = 0x43;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
```

- [ ] **Step 2: Implement ThemeHelper**

```csharp
// src/DesktopTranslation/Helpers/ThemeHelper.cs
using Microsoft.Win32;

namespace DesktopTranslation.Helpers;

public static class ThemeHelper
{
    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool ShouldUseDarkTheme(string themeSetting)
    {
        return themeSetting switch
        {
            "dark" => true,
            "light" => false,
            _ => IsSystemDarkTheme()
        };
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add Win32Interop P/Invoke and ThemeHelper"
```

---

## Task 4: SettingsService

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/SettingsService.cs`
- Test: `tests/DesktopTranslation.Tests/Services/SettingsServiceTests.cs`

- [ ] **Step 1: Write SettingsService tests**

```csharp
// tests/DesktopTranslation.Tests/Services/SettingsServiceTests.cs
using System.Text.Json;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dt_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new SettingsService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var settings = _service.Load();
        Assert.Equal(720, settings.WindowWidth);
        Assert.Equal("google", settings.Engine);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var settings = new AppSettings { Engine = "llm", ApiKey = "test-key" };
        _service.Save(settings);

        var loaded = _service.Load();
        Assert.Equal("llm", loaded.Engine);
        Assert.Equal("test-key", loaded.ApiKey);
    }

    [Fact]
    public void Load_HandlesCorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "not json");
        var settings = _service.Load();
        Assert.Equal(720, settings.WindowWidth);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "SettingsServiceTests"
```

- [ ] **Step 3: Implement SettingsService**

```csharp
// src/DesktopTranslation/Services/SettingsService.cs
using System.IO;
using System.Text.Json;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;

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
        try
        {
            if (!File.Exists(_filePath))
                return new AppSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "SettingsServiceTests"
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add SettingsService with JSON persistence"
```

---

## Task 5: LanguageDetector

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/LanguageDetector.cs`
- Test: `tests/DesktopTranslation.Tests/Services/LanguageDetectorTests.cs`

- [ ] **Step 1: Write LanguageDetector tests**

```csharp
// tests/DesktopTranslation.Tests/Services/LanguageDetectorTests.cs
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class LanguageDetectorTests
{
    [Theory]
    [InlineData("Hello world", "zh-TW")]
    [InlineData("This is a test", "zh-TW")]
    [InlineData("Bonjour le monde", "zh-TW")]
    [InlineData("これはテストです", "zh-TW")]  // Japanese → still targets zh-TW
    public void Detect_NonChinese_ReturnsZhTW(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("你好世界", "en")]
    [InlineData("這是一個測試文字", "en")]
    [InlineData("今天天氣很好，我想出去走走", "en")]
    public void Detect_Chinese_ReturnsEn(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("Hello 你好 world", "zh-TW")]  // Mixed, CJK < 30%
    [InlineData("這是test測試", "en")]           // Mixed, CJK > 30%
    public void Detect_Mixed_UsesThreshold(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_EmptyOrWhitespace_DefaultsToZhTW(string input)
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage(input));
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "LanguageDetectorTests"
```

- [ ] **Step 3: Implement LanguageDetector**

```csharp
// src/DesktopTranslation/Services/LanguageDetector.cs
namespace DesktopTranslation.Services;

public static class LanguageDetector
{
    private const double CjkThreshold = 0.30;

    public static string GetTargetLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "zh-TW";

        var totalChars = 0;
        var cjkChars = 0;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                continue;

            totalChars++;
            if (IsCjk(c))
                cjkChars++;
        }

        if (totalChars == 0)
            return "zh-TW";

        var ratio = (double)cjkChars / totalChars;
        return ratio > CjkThreshold ? "en" : "zh-TW";
    }

    private static bool IsCjk(char c)
    {
        return c >= '\u4E00' && c <= '\u9FFF'    // CJK Unified Ideographs
            || c >= '\u3400' && c <= '\u4DBF'    // CJK Extension A
            || c >= '\uF900' && c <= '\uFAFF';   // CJK Compatibility
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "LanguageDetectorTests"
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add LanguageDetector with CJK Unicode ratio detection"
```

---

## Task 6: Translation Engine Interface & Google Implementation

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/ITranslationEngine.cs`
- Create: `src/DesktopTranslation/Services/GoogleTranslateEngine.cs`
- Test: `tests/DesktopTranslation.Tests/Services/TranslationServiceTests.cs`

- [ ] **Step 1: Create ITranslationEngine interface**

```csharp
// src/DesktopTranslation/Services/ITranslationEngine.cs
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public interface ITranslationEngine
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken ct = default);
}
```

- [ ] **Step 2: Implement GoogleTranslateEngine**

```csharp
// src/DesktopTranslation/Services/GoogleTranslateEngine.cs
using GTranslate.Translators;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class GoogleTranslateEngine : ITranslationEngine
{
    private readonly GoogleTranslator _translator = new();

    public string Name => "Google";

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        try
        {
            var result = await _translator.TranslateAsync(text, targetLanguage);
            return new TranslationResult(
                TranslatedText: result.Translation,
                DetectedSourceLanguage: result.SourceLanguage.ISO6391 ?? "unknown",
                IsSuccess: true);
        }
        catch (Exception ex)
        {
            return new TranslationResult(
                TranslatedText: "",
                DetectedSourceLanguage: "unknown",
                IsSuccess: false,
                ErrorMessage: ex.Message);
        }
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add ITranslationEngine interface and GoogleTranslateEngine"
```

---

## Task 7: LLM Translation Engine

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/LlmTranslateEngine.cs`

- [ ] **Step 1: Implement LlmTranslateEngine**

```csharp
// src/DesktopTranslation/Services/LlmTranslateEngine.cs
using System.ClientModel;
using DesktopTranslation.Models;
using OpenAI;
using OpenAI.Chat;

namespace DesktopTranslation.Services;

public class LlmTranslateEngine : ITranslationEngine
{
    private readonly string _provider;
    private readonly string _apiKey;

    public LlmTranslateEngine(string provider, string apiKey)
    {
        _provider = provider;
        _apiKey = apiKey;
    }

    public string Name => $"LLM ({_provider})";

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        try
        {
            var targetName = targetLanguage == "en" ? "English" : "Traditional Chinese (zh-TW)";
            var systemPrompt = $"You are a translator. Translate the following text to {targetName}. " +
                               "Output ONLY the translation, no explanations.";

            if (_provider == "openai")
            {
                return await TranslateWithOpenAiAsync(systemPrompt, text, ct);
            }
            else
            {
                return await TranslateWithClaudeAsync(systemPrompt, text, ct);
            }
        }
        catch (Exception ex)
        {
            return new TranslationResult("", "unknown", false, ex.Message);
        }
    }

    private async Task<TranslationResult> TranslateWithOpenAiAsync(
        string systemPrompt, string text, CancellationToken ct)
    {
        var client = new OpenAIClient(new ApiKeyCredential(_apiKey));
        var chatClient = client.GetChatClient("gpt-4o-mini");
        var response = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(text)
            ],
            cancellationToken: ct);

        var translated = response.Value.Content[0].Text;
        return new TranslationResult(translated, "auto", true);
    }

    private async Task<TranslationResult> TranslateWithClaudeAsync(
        string systemPrompt, string text, CancellationToken ct)
    {
        var client = new Claudia.Anthropic { ApiKey = _apiKey };
        var response = await client.Messages.CreateAsync(new()
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 4096,
            System = systemPrompt,
            Messages = [new() { Role = "user", Content = text }]
        }, ct);

        var translated = response.Content[0].Text;
        return new TranslationResult(translated, "auto", true);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add LlmTranslateEngine with Claude and OpenAI support"
```

---

## Task 8: TranslationService (Dispatcher) with Polly Retry

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/TranslationService.cs`

- [ ] **Step 1: Implement TranslationService**

```csharp
// src/DesktopTranslation/Services/TranslationService.cs
using DesktopTranslation.Models;
using Polly;
using Polly.Retry;

namespace DesktopTranslation.Services;

public class TranslationService
{
    private readonly Dictionary<string, ITranslationEngine> _engines = new();
    private readonly ResiliencePipeline _retryPipeline;

    public string CurrentEngineName { get; private set; } = "google";

    public TranslationService()
    {
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    public void RegisterEngine(string key, ITranslationEngine engine)
    {
        _engines[key] = engine;
    }

    public void SetEngine(string key)
    {
        if (_engines.ContainsKey(key))
            CurrentEngineName = key;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        if (!_engines.TryGetValue(CurrentEngineName, out var engine))
            return new TranslationResult("", "unknown", false, "No engine configured");

        try
        {
            return await _retryPipeline.ExecuteAsync(
                async token => await engine.TranslateAsync(text, targetLanguage, token),
                ct);
        }
        catch (Exception ex)
        {
            return new TranslationResult("", "unknown", false, $"Translation failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add TranslationService dispatcher with Polly retry"
```

---

## Task 9: HotkeyService (Double Ctrl+C Detection)

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/HotkeyService.cs`

- [ ] **Step 1: Implement HotkeyService**

```csharp
// src/DesktopTranslation/Services/HotkeyService.cs
using System.Diagnostics;
using System.Runtime.InteropServices;
using DesktopTranslation.Helpers;

namespace DesktopTranslation.Services;

public class HotkeyService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly Win32Interop.LowLevelKeyboardProc _hookProc;
    private DateTime _lastCtrlCTime = DateTime.MinValue;
    private bool _ctrlPressed;
    private int _doubleTapInterval;

    public event Action? DoubleCopyDetected;

    public HotkeyService(int doubleTapInterval = 400)
    {
        _doubleTapInterval = doubleTapInterval;
        _hookProc = HookCallback;
    }

    public void UpdateInterval(int interval) => _doubleTapInterval = interval;

    public void Start()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookId = Win32Interop.SetWindowsHookEx(
            Win32Interop.WH_KEYBOARD_LL,
            _hookProc,
            Win32Interop.GetModuleHandle(module.ModuleName),
            0);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Interop.KBDLLHOOKSTRUCT>(lParam);
            var isKeyDown = wParam == (IntPtr)Win32Interop.WM_KEYDOWN
                         || wParam == (IntPtr)Win32Interop.WM_SYSKEYDOWN;
            var isKeyUp = wParam == (IntPtr)0x0101; // WM_KEYUP

            if (hookStruct.vkCode == Win32Interop.VK_CONTROL)
            {
                _ctrlPressed = isKeyDown;
            }
            else if (hookStruct.vkCode == Win32Interop.VK_C && isKeyDown && _ctrlPressed)
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _lastCtrlCTime).TotalMilliseconds;

                if (elapsed < _doubleTapInterval && elapsed > 50) // > 50ms to ignore key repeat
                {
                    _lastCtrlCTime = DateTime.MinValue;
                    // Fire event on UI thread — keep hook callback fast
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                        DoubleCopyDetected?.Invoke());
                }
                else
                {
                    _lastCtrlCTime = now;
                }
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add HotkeyService with low-level keyboard hook for double Ctrl+C"
```

---

## Task 10: ClipboardService

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/ClipboardService.cs`

- [ ] **Step 1: Implement ClipboardService**

```csharp
// src/DesktopTranslation/Services/ClipboardService.cs
using System.Windows;

namespace DesktopTranslation.Services;

public class ClipboardService
{
    public string? GetText()
    {
        try
        {
            if (Clipboard.ContainsText())
                return Clipboard.GetText();
            return null;
        }
        catch
        {
            return null;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add -A
git commit -m "feat: add ClipboardService"
```

---

## Task 11: TtsService

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/TtsService.cs`

- [ ] **Step 1: Implement TtsService**

```csharp
// src/DesktopTranslation/Services/TtsService.cs
using System.Speech.Synthesis;

namespace DesktopTranslation.Services;

public class TtsService : IDisposable
{
    private readonly SpeechSynthesizer _synth = new();
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public TtsService()
    {
        _synth.SpeakCompleted += (_, _) => _isSpeaking = false;
    }

    public void SetSpeed(double speed)
    {
        // Rate ranges from -10 to 10, default 0
        _synth.Rate = (int)Math.Clamp((speed - 1.0) * 10, -10, 10);
    }

    public void Speak(string text, string language)
    {
        Stop();
        SelectVoiceForLanguage(language);
        _isSpeaking = true;
        _synth.SpeakAsync(text);
    }

    public void Stop()
    {
        if (_isSpeaking)
        {
            _synth.SpeakAsyncCancelAll();
            _isSpeaking = false;
        }
    }

    private void SelectVoiceForLanguage(string language)
    {
        try
        {
            var culture = language.StartsWith("zh") ? "zh-TW" : "en-US";
            _synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult,
                0, new System.Globalization.CultureInfo(culture));
        }
        catch
        {
            // Fallback to default voice
        }
    }

    public void Dispose()
    {
        _synth.Dispose();
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add TtsService with System.Speech"
```

---

## Task 12: HistoryService

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/HistoryService.cs`
- Test: `tests/DesktopTranslation.Tests/Services/HistoryServiceTests.cs`

- [ ] **Step 1: Write HistoryService tests**

```csharp
// tests/DesktopTranslation.Tests/Services/HistoryServiceTests.cs
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class HistoryServiceTests
{
    [Fact]
    public void Add_StoresEntry()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "hello", "你好", "en", "zh-TW", "google", DateTime.UtcNow));

        Assert.Single(service.GetAll());
    }

    [Fact]
    public void Add_BeyondMax_RemovesOldest()
    {
        var service = new HistoryService(maxEntries: 3);

        for (int i = 0; i < 5; i++)
            service.Add(new TranslationHistoryEntry(
                $"text{i}", $"translated{i}", "en", "zh-TW", "google", DateTime.UtcNow));

        var all = service.GetAll();
        Assert.Equal(3, all.Count);
        Assert.Equal("text2", all[0].SourceText);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "hello", "你好", "en", "zh-TW", "google", DateTime.UtcNow));

        service.Clear();
        Assert.Empty(service.GetAll());
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "HistoryServiceTests"
```

- [ ] **Step 3: Implement HistoryService**

```csharp
// src/DesktopTranslation/Services/HistoryService.cs
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class HistoryService
{
    private readonly List<TranslationHistoryEntry> _entries = new();
    private readonly int _maxEntries;

    public HistoryService(int maxEntries = 50)
    {
        _maxEntries = maxEntries;
    }

    public IReadOnlyList<TranslationHistoryEntry> GetAll() => _entries.AsReadOnly();

    public void Add(TranslationHistoryEntry entry)
    {
        _entries.Add(entry);
        while (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);
    }

    public void Clear() => _entries.Clear();
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
dotnet test tests/DesktopTranslation.Tests --filter "HistoryServiceTests"
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add HistoryService with max 50 entries"
```

---

## Task 13: AutoStartService

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Services/AutoStartService.cs`

- [ ] **Step 1: Implement AutoStartService**

```csharp
// src/DesktopTranslation/Services/AutoStartService.cs
using Microsoft.Win32;

namespace DesktopTranslation.Services;

public static class AutoStartService
{
    private const string AppName = "DesktopTranslation";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? "";
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add AutoStartService with Registry Run Key"
```

---

## Task 14: Main Translation Window (XAML)

**Team:** Dev + Design
**Files:**
- Create: `src/DesktopTranslation/Views/TranslationWindow.xaml`
- Create: `src/DesktopTranslation/Views/TranslationWindow.xaml.cs`

- [ ] **Step 1: Create TranslationWindow XAML**

Create `src/DesktopTranslation/Views/TranslationWindow.xaml` with:
- Borderless window, 720x400, AllowsTransparency, rounded corners 12px
- Custom title bar (32px): drag area, Segmented Control (Google|LLM), pin/minimize/close buttons
- Left pane: detected language label, TTS button, clear button, editable TextBox
- Right pane: target language label, TTS button, copy button, read-only TextBlock
- Shimmer loading skeleton
- Bottom expandable history panel
- Light/dark theme resource dictionaries
- Drop shadow effect (Blur 20px, Opacity 0.25, Y-offset 4px)
- Fade-in + scale animation on show (150ms CubicOut)
- Fade-out on hide (100ms)

- [ ] **Step 2: Create TranslationWindow code-behind**

`src/DesktopTranslation/Views/TranslationWindow.xaml.cs` with:
- Window dragging via title bar MouseLeftButtonDown
- Remember position on close (save to SettingsService)
- Restore position on open
- Pin toggle (Topmost property)
- Engine switch handler
- Input TextBox debounce (500ms) for re-translation on edit
- TTS play/stop for both panes
- Copy button handler
- History panel expand/collapse toggle
- Theme switching based on settings

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add TranslationWindow with floating UI and dark/light theme"
```

---

## Task 15: Settings Window

**Team:** Dev + Design
**Files:**
- Create: `src/DesktopTranslation/Views/SettingsWindow.xaml`
- Create: `src/DesktopTranslation/Views/SettingsWindow.xaml.cs`

- [ ] **Step 1: Create SettingsWindow**

Single-page settings window with:
- Translation engine radio buttons (Google / LLM)
- LLM provider dropdown (Claude / OpenAI)
- API Key PasswordBox
- Auto-start toggle switch
- Double-tap interval slider (200-800ms, default 400)
- TTS speed slider (0.5-2.0, default 1.0)
- Theme selector (System / Light / Dark)
- Save and Cancel buttons

- [ ] **Step 2: Code-behind with save/cancel logic**

- Load current AppSettings on open
- Save button: write new AppSettings via SettingsService, apply changes, close
- Cancel button: discard and close

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add SettingsWindow"
```

---

## Task 16: TrayIconManager

**Team:** Dev
**Files:**
- Create: `src/DesktopTranslation/Views/TrayIconManager.cs`

- [ ] **Step 1: Implement TrayIconManager**

```csharp
// src/DesktopTranslation/Views/TrayIconManager.cs
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;

namespace DesktopTranslation.Views;

public class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onOpenSettings;
    private readonly Action<string> _onSwitchEngine;
    private readonly Action<bool> _onToggleAutoStart;
    private readonly Action _onExit;

    public TrayIconManager(
        Action onShowWindow,
        Action onOpenSettings,
        Action<string> onSwitchEngine,
        Action<bool> onToggleAutoStart,
        Action onExit,
        bool autoStartEnabled,
        string currentEngine)
    {
        _onShowWindow = onShowWindow;
        _onOpenSettings = onOpenSettings;
        _onSwitchEngine = onSwitchEngine;
        _onToggleAutoStart = onToggleAutoStart;
        _onExit = onExit;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "DesktopTranslation",
            ContextMenu = BuildContextMenu(autoStartEnabled, currentEngine)
        };

        _trayIcon.TrayLeftMouseDown += (_, _) => _onShowWindow();
    }

    public void UpdateMenu(bool autoStartEnabled, string currentEngine)
    {
        _trayIcon.ContextMenu = BuildContextMenu(autoStartEnabled, currentEngine);
    }

    private ContextMenu BuildContextMenu(bool autoStartEnabled, string currentEngine)
    {
        var menu = new ContextMenu();

        var showItem = new MenuItem { Header = "顯示主視窗" };
        showItem.Click += (_, _) => _onShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new Separator());

        var engineMenu = new MenuItem { Header = "翻譯引擎" };
        var googleItem = new MenuItem
        {
            Header = "Google",
            IsCheckable = true,
            IsChecked = currentEngine == "google"
        };
        googleItem.Click += (_, _) => _onSwitchEngine("google");
        var llmItem = new MenuItem
        {
            Header = "LLM",
            IsCheckable = true,
            IsChecked = currentEngine == "llm"
        };
        llmItem.Click += (_, _) => _onSwitchEngine("llm");
        engineMenu.Items.Add(googleItem);
        engineMenu.Items.Add(llmItem);
        menu.Items.Add(engineMenu);

        menu.Items.Add(new Separator());

        var autoStartItem = new MenuItem
        {
            Header = "開機自啟動",
            IsCheckable = true,
            IsChecked = autoStartEnabled
        };
        autoStartItem.Click += (_, _) => _onToggleAutoStart(!autoStartEnabled);
        menu.Items.Add(autoStartItem);

        var settingsItem = new MenuItem { Header = "設定" };
        settingsItem.Click += (_, _) => _onOpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = "關於" };
        aboutItem.Click += (_, _) => MessageBox.Show(
            "DesktopTranslation v1.0\n雙擊 Ctrl+C 快速翻譯",
            "關於", MessageBoxButton.OK, MessageBoxImage.Information);
        menu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = "結束" };
        exitItem.Click += (_, _) => _onExit();
        menu.Items.Add(exitItem);

        return menu;
    }

    public void Dispose()
    {
        _trayIcon.Dispose();
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add TrayIconManager with context menu"
```

---

## Task 17: App Entry Point — Wire Everything Together

**Team:** Dev
**Files:**
- Modify: `src/DesktopTranslation/App.xaml`
- Modify: `src/DesktopTranslation/App.xaml.cs`

- [ ] **Step 1: Update App.xaml**

Set `ShutdownMode="OnExplicitShutdown"` and remove `StartupUri` (we manage window lifecycle manually).

- [ ] **Step 2: Update App.xaml.cs**

Wire all services together:
- Initialize SettingsService, load settings
- Create HotkeyService with interval from settings
- Create ClipboardService
- Create TranslationService, register Google and LLM engines
- Create TtsService
- Create HistoryService
- Create TrayIconManager
- On DoubleCopyDetected: read clipboard → detect language → translate → show TranslationWindow
- Handle show/hide, settings, engine switch, auto-start toggle, exit
- On exit: save settings, dispose all services

- [ ] **Step 3: Verify full build**

```bash
dotnet build
```

- [ ] **Step 4: Manual smoke test**

```bash
dotnet run --project src/DesktopTranslation
```
- Verify tray icon appears
- Verify double Ctrl+C triggers translation window
- Verify Google translate works
- Verify window remembers position
- Verify TTS works
- Verify settings window opens

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: wire App entry point with all services"
```

---

## Task 18: Run All Tests & Final Verification

**Team:** QA
**Files:** All test files

- [ ] **Step 1: Run full test suite**

```bash
dotnet test tests/DesktopTranslation.Tests -v normal
```
Expected: All tests pass.

- [ ] **Step 2: Run build in Release mode**

```bash
dotnet build -c Release
```

- [ ] **Step 3: Publish self-contained**

```bash
dotnet publish src/DesktopTranslation -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```

- [ ] **Step 4: Verify published exe launches**

```bash
./publish/DesktopTranslation.exe
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: verify build and publish pipeline"
```

---

## Task 19: Inno Setup Installer

**Team:** Dev
**Files:**
- Create: `installer/setup.iss`

- [ ] **Step 1: Create Inno Setup script**

```iss
; installer/setup.iss
[Setup]
AppName=DesktopTranslation
AppVersion=1.0.0
DefaultDirName={autopf}\DesktopTranslation
DefaultGroupName=DesktopTranslation
OutputDir=..\dist
OutputBaseFilename=DesktopTranslation-Setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\DesktopTranslation.exe
PrivilegesRequired=lowest

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\DesktopTranslation"; Filename: "{app}\DesktopTranslation.exe"
Name: "{group}\Uninstall DesktopTranslation"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DesktopTranslation"; Filename: "{app}\DesktopTranslation.exe"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DesktopTranslation"; Flags: uninsdeletevalue dontcreatekey

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\DesktopTranslation"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox('要保留設定檔嗎？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // User wants to keep settings, remove the UninstallDelete directive effect
      // Settings are in %AppData%\DesktopTranslation
    end;
  end;
end;
```

- [ ] **Step 2: Commit**

```bash
git add -A
git commit -m "chore: add Inno Setup installer script"
```
