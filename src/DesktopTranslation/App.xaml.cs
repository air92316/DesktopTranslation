using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using DesktopTranslation.Helpers;
using DesktopTranslation.Models;
using DesktopTranslation.Services;
using DesktopTranslation.Views;

namespace DesktopTranslation;

public partial class App : System.Windows.Application
{
    private SettingsService _settingsService = null!;
    private HotkeyService _hotkeyService = null!;
    private ClipboardService _clipboardService = null!;
    private TranslationService _translationService = null!;
    private TtsService _ttsService = null!;
    private HistoryService _historyService = null!;
    private TrayIconManager _trayIconManager = null!;
    private UpdateService _updateService = null!;
    private TranslationWindow? _translationWindow;
    private UpdateNotificationWindow? _updateWindow;
    private bool _updateCheckInProgress;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers
        DispatcherUnhandledException += (s, args) =>
        {
            Debug.WriteLine($"[UNHANDLED UI] {args.Exception}");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Debug.WriteLine($"[UNHANDLED] {args.ExceptionObject}");
        };

        try
        {
            Debug.WriteLine("[STARTUP] Initializing...");

            // Initialize services
            _settingsService = new SettingsService();
            _settings = _settingsService.Load();
            Debug.WriteLine($"[STARTUP] Settings loaded. Engine={_settings.Engine}, Theme={_settings.Theme}");

            // Apply theme at app level
            ApplyAppTheme(_settings.Theme);
            Debug.WriteLine("[STARTUP] Theme applied");

            _clipboardService = new ClipboardService();
            _historyService = new HistoryService();
            _ttsService = new TtsService();
            _ttsService.SetSpeed(_settings.TtsSpeed);

            // Translation engines
            _translationService = new TranslationService();
            _translationService.RegisterEngine("google", new GoogleTranslateEngine());

            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _translationService.RegisterEngine("llm",
                    new LlmTranslateEngine(_settings.LlmProvider, _settings.ApiKey));
            }

            _translationService.SetEngine(_settings.Engine);
            Debug.WriteLine("[STARTUP] Translation services ready");

            // Hotkey service
            _hotkeyService = new HotkeyService(_settings.DoubleTapInterval);
            _hotkeyService.DoubleCopyDetected += OnDoubleCopyDetected;
            _hotkeyService.Start();
            Debug.WriteLine("[STARTUP] Hotkey service started");

            // Update service
            _updateService = new UpdateService();
            _updateService.CleanupOldInstallers();
            Debug.WriteLine("[STARTUP] Update service ready");

            // Sync auto-start registry with settings (fixes stale/missing entries)
            AutoStartService.SyncRegistry(_settings.AutoStart);

            // Tray icon
            _trayIconManager = new TrayIconManager(
                onShowWindow: ShowTranslationWindow,
                onOpenSettings: OpenSettings,
                onSwitchEngine: SwitchEngine,
                onToggleAutoStart: ToggleAutoStart,
                onCheckUpdate: CheckForUpdateManual,
                onExit: ExitApp,
                autoStartEnabled: _settings.AutoStart,
                currentEngine: _settings.Engine);
            Debug.WriteLine("[STARTUP] Tray icon created");

            // Create a hidden window to keep the WPF message pump alive.
            // Without this, WH_KEYBOARD_LL hooks and tray icon clicks won't work
            // because the Dispatcher has no window to pump messages for.
            var hiddenWindow = new Window
            {
                Width = 0, Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Visibility = Visibility.Hidden
            };
            hiddenWindow.Show();
            hiddenWindow.Hide();
            MainWindow = hiddenWindow;
            Debug.WriteLine("[STARTUP] Hidden message pump window created — app is ready!");

            // Background update check
            if (_settings.AutoUpdateEnabled)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await Dispatcher.InvokeAsync(() => CheckForUpdateInBackground());
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[STARTUP FATAL] {ex}");
            System.Windows.MessageBox.Show($"啟動失敗：{ex.Message}", "DesktopTranslation 錯誤",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDoubleCopyDetected()
    {
        var text = _clipboardService.GetText();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var targetLanguage = LanguageDetector.GetTargetLanguage(text);
        EnsureTranslationWindow();
        _translationWindow!.ShowWithTranslation(text, targetLanguage);
    }

    private void ShowTranslationWindow()
    {
        EnsureTranslationWindow();

        if (_translationWindow!.IsVisible && _translationWindow.Opacity > 0.5)
        {
            // Window is fully visible — hide it
            _translationWindow.HideWindow();
        }
        else
        {
            // Window is hidden or fading out — show it
            _translationWindow.Opacity = 1;
            _translationWindow.Show();
            _translationWindow.Activate();

            try
            {
                var fadeIn = (System.Windows.Media.Animation.Storyboard)
                    _translationWindow.FindResource("FadeInStoryboard");
                fadeIn.Begin(_translationWindow);
            }
            catch { /* animation not critical */ }
        }
    }

    private void EnsureTranslationWindow()
    {
        if (_translationWindow == null)
        {
            _translationWindow = new TranslationWindow(
                _translationService, _ttsService, _historyService, _settingsService);
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsService, OnSettingsApplied);
        settingsWindow.ShowDialog();
    }

    private void OnSettingsApplied(AppSettings newSettings)
    {
        _settings = newSettings;

        // Update hotkey interval
        _hotkeyService.UpdateInterval(newSettings.DoubleTapInterval);

        // Update TTS speed
        _ttsService.SetSpeed(newSettings.TtsSpeed);

        // Update LLM engine if API key changed
        if (!string.IsNullOrEmpty(newSettings.ApiKey))
        {
            _translationService.RegisterEngine("llm",
                new LlmTranslateEngine(newSettings.LlmProvider, newSettings.ApiKey));
        }
        _translationService.SetEngine(newSettings.Engine);

        // Update theme at app level (affects all windows via DynamicResource)
        ApplyAppTheme(newSettings.Theme);

        // Update tray menu
        _trayIconManager.UpdateMenu(newSettings.AutoStart, newSettings.Engine);

        // Refresh LLM availability in translation window
        _translationWindow?.RefreshLlmAvailability();
    }

    private void SwitchEngine(string engine)
    {
        _translationService.SetEngine(engine);
        var settings = _settingsService.Load();
        _settingsService.Save(settings with { Engine = engine });
        _trayIconManager.UpdateMenu(settings.AutoStart, engine);
    }

    private void ToggleAutoStart(bool enabled)
    {
        AutoStartService.SetEnabled(enabled);
        var settings = _settingsService.Load();
        _settingsService.Save(settings with { AutoStart = enabled });
        _trayIconManager.UpdateMenu(enabled, settings.Engine);
    }

    private void ExitApp()
    {
        // Save window position if visible
        if (_translationWindow?.IsVisible == true)
        {
            _translationWindow.HideWindow();
        }

        // Disposal happens in OnExit — just trigger shutdown
        Shutdown();
    }

    private async void CheckForUpdateInBackground()
    {
        try
        {
            if (_updateCheckInProgress)
            {
                Debug.WriteLine("[UPDATE] Check already in progress, skipping");
                return;
            }
            _updateCheckInProgress = true;

            var settings = _settingsService.Load();
            var elapsed = DateTime.Now - settings.LastUpdateCheck;
            if (elapsed.TotalHours < settings.UpdateCheckIntervalHours)
            {
                Debug.WriteLine("[UPDATE] Skipped — last check was too recent");
                return;
            }

            var currentVersion = GetCurrentVersion();
            var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion);

            _settingsService.Save(settings with { LastUpdateCheck = DateTime.Now });

            if (updateInfo is null)
            {
                Debug.WriteLine("[UPDATE] Already up to date");
                return;
            }

            // Skip if user chose to skip this version
            var latestSettings = _settingsService.Load();
            var skippedTag = latestSettings.SkippedVersion.TrimStart('v', 'V');
            var newTag = updateInfo.Version.TrimStart('v', 'V');
            if (string.Equals(skippedTag, newTag, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[UPDATE] Version {updateInfo.Version} is skipped by user");
                return;
            }

            if (_updateWindow?.IsVisible == true)
                return;
            _updateWindow = new UpdateNotificationWindow(_updateService, updateInfo, _settingsService);
            _updateWindow.Closed += (_, _) => _updateWindow = null;
            _updateWindow.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UPDATE] Background check failed: {ex.Message}");
        }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private async void CheckForUpdateManual()
    {
        if (_updateCheckInProgress)
        {
            System.Windows.MessageBox.Show("正在檢查更新中，請稍候。", "檢查更新",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _updateCheckInProgress = true;

        try
        {
            var currentVersion = GetCurrentVersion();
            var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion);

            var settings = _settingsService.Load();
            _settingsService.Save(settings with { LastUpdateCheck = DateTime.Now });

            if (updateInfo is null)
            {
                System.Windows.MessageBox.Show(
                    "目前已是最新版本。",
                    "檢查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_updateWindow?.IsVisible == true)
            {
                _updateWindow.Activate();
                return;
            }
            _updateWindow = new UpdateNotificationWindow(_updateService, updateInfo, _settingsService);
            _updateWindow.Closed += (_, _) => _updateWindow = null;
            _updateWindow.Show();
        }
        catch (Exception ex)
        {
            var userMessage = ex switch
            {
                HttpRequestException => "無法連接到更新伺服器，請檢查網路連線。",
                TaskCanceledException => "連線逾時，請稍後再試。",
                _ => $"檢查更新時發生錯誤，請稍後再試。"
            };
            Debug.WriteLine($"[UPDATE] Manual check failed: {ex}");
            System.Windows.MessageBox.Show(
                userMessage,
                "更新錯誤",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private static string GetCurrentVersion()
    {
        var version = typeof(App).Assembly.GetName().Version;
        return $"{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    public void ApplyAppTheme(string themeSetting)
    {
        var isDark = ThemeHelper.ShouldUseDarkTheme(themeSetting);
        var themeUri = isDark
            ? new Uri("Themes/JapaneseDark.xaml", UriKind.Relative)
            : new Uri("Themes/JapaneseLight.xaml", UriKind.Relative);

        var merged = Resources.MergedDictionaries;

        // Replace the first dictionary (theme) while keeping GlobalStyles
        if (merged.Count > 0)
        {
            merged[0] = new ResourceDictionary { Source = themeUri };
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _ttsService?.Dispose();
        _trayIconManager?.Dispose();
        base.OnExit(e);
    }
}
