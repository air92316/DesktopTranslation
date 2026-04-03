# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                    # Build entire solution
dotnet test                                     # Run all 180 tests
dotnet test --filter "FullyQualifiedName~UpdateServiceTests"  # Run single test class
dotnet test --filter "IsNewerVersion"            # Run tests matching name
dotnet run --project src/DesktopTranslation      # Run the app (DPI manifest ignored under dotnet.exe)
dotnet publish src/DesktopTranslation -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o publish/
```

Inno Setup installer (requires local install):
```bash
"/c/Users/User/AppData/Local/Programs/Inno Setup 6/ISCC.exe" installer/setup.iss
```

GitHub CLI is at `"/c/Program Files/GitHub CLI/gh.exe"` (not in PowerShell PATH).

## Architecture

**WPF system-tray application** — no main window on startup. A hidden window keeps the WPF Dispatcher message pump alive (required for keyboard hooks and tray icon).

### Service Wiring (Manual DI in App.xaml.cs)

All services are instantiated in `App.OnStartup` and passed to windows via constructors. No DI container.

Initialization order matters:
1. `SettingsService` → loads/decrypts settings from `%APPDATA%/DesktopTranslation/settings.json`
2. Theme applied via `ResourceDictionary` swap
3. `TranslationService` → registers engines ("google" always, "llm" if API key exists)
4. `HotkeyService` → installs Win32 `WH_KEYBOARD_LL` global hook
5. `UpdateService` → GitHub Releases API checker
6. `TrayIconManager` → WinForms `NotifyIcon` (not WPF's H.NotifyIcon — it's unreliable on .NET 8)
7. Hidden window created to pump messages

### Keyboard Hook

Uses Win32 `WH_KEYBOARD_LL` via P/Invoke. Must use `IntPtr.Zero` as hMod (not `GetModuleHandle` — broken in .NET 8). Must detect `VK_LCONTROL`/`VK_RCONTROL` (0xA2/0xA3), not `VK_CONTROL` (0x11). Hook callback must return fast (<300ms) — translation work is dispatched via `Dispatcher.BeginInvoke`.

`DoubleTapDetector` handles the timing logic (extracted for testability).

### DPI Scaling

App manifest declares `PerMonitorV2`, but `dotnet run` ignores it. Runtime DPI is read via `Win32Interop.GetSystemDpiScale()` using `GetDeviceCaps(LOGPIXELSX)`. Windows apply `LayoutTransform` **and** scale `Width`/`Height` — both are required (LayoutTransform alone causes content overflow).

### Auto-Update System

`UpdateService` queries `https://api.github.com/repos/air92316/DesktopTranslation/releases/latest`. GitHub API returns **snake_case** JSON — DTOs require `[JsonPropertyName("tag_name")]` attributes (not just `PropertyNameCaseInsensitive`).

Update flow: API check (3 retries with backoff) → URL domain validation (github.com only) → download to %TEMP% → file size verification → flush stream before validation → path sandboxing → Inno Setup `/SILENT` install.

`UpdateNotificationWindow` must use `Show()` (not `ShowDialog()`) — modal dialogs block the entire WPF Dispatcher, freezing hotkeys and tray.

### Translation Engines

`ITranslationEngine` interface with two implementations:
- `GoogleTranslateEngine` — GTranslate (free, no API key)
- `LlmTranslateEngine` — Claude/OpenAI with prompt injection defense

`TranslationService` wraps engines with Polly retry (2 attempts, 10s timeout, exponential backoff).

### Settings & Security

`SettingsService` stores JSON at `%APPDATA%/DesktopTranslation/settings.json`. API keys are DPAPI-encrypted (`DataProtectionHelper`). `AppSettings` is an immutable `record` with `init` properties — use `with` expressions to update. `SettingsService.Validate()` clamps numeric values to safe ranges.

### Theme System

Two `ResourceDictionary` XAML files (JapaneseLight, JapaneseDark) + GlobalStyles. Theme switching works by replacing `MergedDictionaries[0]` at the `Application` level. All windows use `DynamicResource` bindings.

## WPF + WinForms Coexistence

The project uses both WPF and WinForms (`<UseWindowsForms>true</UseWindowsForms>`). This causes namespace collisions on `Clipboard`, `MessageBox`, `Brush`, `FontStyle` — always use fully qualified names (e.g., `System.Windows.MessageBox`).

## Release Process

Version is defined in csproj `<Version>` and must be synced to 3 locations:
1. `src/DesktopTranslation/DesktopTranslation.csproj` — source of truth (Assembly reads this)
2. `installer/setup.iss` — `#define MyAppVersion`
3. `website/index.html` — badge + download URL

`scripts/release.ps1` automates this. Website deploys to Cloudflare Pages with `--branch main` (not master).

Website download button uses GitHub's `/releases/latest/download/` URL which auto-redirects to the newest asset.

## Testing

xUnit with `Assert` (not FluentAssertions). `InternalsVisibleTo` is set for `DesktopTranslation.Tests`. Test files mirror src structure under `tests/`. `internal` methods like `IsValidDownloadUrl` and `FormatFileSize` are testable.

`UpdateService` uses a static `HttpClient` — testing `CheckForUpdateAsync`/`DownloadUpdateAsync` requires refactoring to inject `HttpMessageHandler`.
