using System.Windows;
using DesktopTranslation.Helpers;
using DesktopTranslation.Models;
using DesktopTranslation.Services;
using DesktopTranslation.Views;

namespace DesktopTranslation;

public partial class App : Application
{
    private SettingsService _settingsService = null!;
    private HotkeyService _hotkeyService = null!;
    private ClipboardService _clipboardService = null!;
    private TranslationService _translationService = null!;
    private TtsService _ttsService = null!;
    private HistoryService _historyService = null!;
    private TrayIconManager _trayIconManager = null!;
    private TranslationWindow? _translationWindow;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _settingsService = new SettingsService();
        _settings = _settingsService.Load();

        // Apply theme at app level
        ApplyAppTheme(_settings.Theme);

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

        // Hotkey service
        _hotkeyService = new HotkeyService(_settings.DoubleTapInterval);
        _hotkeyService.DoubleCopyDetected += OnDoubleCopyDetected;
        _hotkeyService.Start();

        // Tray icon
        _trayIconManager = new TrayIconManager(
            onShowWindow: ShowTranslationWindow,
            onOpenSettings: OpenSettings,
            onSwitchEngine: SwitchEngine,
            onToggleAutoStart: ToggleAutoStart,
            onExit: ExitApp,
            autoStartEnabled: _settings.AutoStart,
            currentEngine: _settings.Engine);
    }

    private void OnDoubleCopyDetected()
    {
        var text = _clipboardService.GetText();
        if (string.IsNullOrWhiteSpace(text)) return;

        var targetLanguage = LanguageDetector.GetTargetLanguage(text);
        EnsureTranslationWindow();
        _translationWindow!.ShowWithTranslation(text, targetLanguage);
    }

    private void ShowTranslationWindow()
    {
        if (_translationWindow == null || !_translationWindow.IsVisible)
        {
            EnsureTranslationWindow();
            _translationWindow!.Show();
            _translationWindow.Activate();
        }
        else
        {
            _translationWindow.HideWindow();
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
