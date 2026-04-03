using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Views;

public partial class TranslationWindow : Window
{
    private readonly TranslationService _translationService;
    private readonly TtsService _ttsService;
    private readonly HistoryService _historyService;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _debounceTimer;

    private string _currentSourceText = "";
    private string _currentTargetLanguage = "zh-TW";
    private string _currentSourceLanguage = "";
    private bool _historyExpanded;
    private bool _isTranslating;
    private bool _suppressLanguageChangeEvent;

    // Language options: (code, display name)
    private static readonly (string Code, string Name)[] SourceLanguages =
    [
        ("auto", "自動偵測"),
        ("en", "English"),
        ("zh-TW", "繁體中文"),
        ("zh-CN", "簡體中文"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("es", "Español"),
        ("pt", "Português"),
        ("ru", "Русский"),
        ("th", "ไทย"),
        ("vi", "Tiếng Việt"),
        ("ar", "العربية"),
    ];

    private static readonly (string Code, string Name)[] TargetLanguages =
    [
        ("zh-TW", "繁體中文"),
        ("en", "English"),
        ("zh-CN", "簡體中文"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("es", "Español"),
        ("pt", "Português"),
        ("ru", "Русский"),
        ("th", "ไทย"),
        ("vi", "Tiếng Việt"),
        ("ar", "العربية"),
    ];

    public TranslationWindow(
        TranslationService translationService,
        TtsService ttsService,
        HistoryService historyService,
        SettingsService settingsService)
    {
        InitializeComponent();

        _translationService = translationService;
        _ttsService = ttsService;
        _historyService = historyService;
        _settingsService = settingsService;

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        InitLanguageComboBoxes();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void InitLanguageComboBoxes()
    {
        _suppressLanguageChangeEvent = true;

        SourceLanguageCombo.Items.Clear();
        foreach (var (code, name) in SourceLanguages)
            SourceLanguageCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = name,
                Tag = code
            });
        SourceLanguageCombo.SelectedIndex = 0; // auto

        TargetLanguageCombo.Items.Clear();
        foreach (var (code, name) in TargetLanguages)
            TargetLanguageCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = name,
                Tag = code
            });
        TargetLanguageCombo.SelectedIndex = 0; // zh-TW

        _suppressLanguageChangeEvent = false;
    }

    private void SetSourceLanguageCombo(string code)
    {
        _suppressLanguageChangeEvent = true;
        for (int i = 0; i < SourceLanguageCombo.Items.Count; i++)
        {
            if (SourceLanguageCombo.Items[i] is System.Windows.Controls.ComboBoxItem item
                && (string)item.Tag == code)
            {
                SourceLanguageCombo.SelectedIndex = i;
                break;
            }
        }
        _suppressLanguageChangeEvent = false;
    }

    private void SetTargetLanguageCombo(string code)
    {
        _suppressLanguageChangeEvent = true;
        for (int i = 0; i < TargetLanguageCombo.Items.Count; i++)
        {
            if (TargetLanguageCombo.Items[i] is System.Windows.Controls.ComboBoxItem item
                && (string)item.Tag == code)
            {
                TargetLanguageCombo.SelectedIndex = i;
                break;
            }
        }
        _suppressLanguageChangeEvent = false;
    }

    private string GetSelectedSourceCode() =>
        SourceLanguageCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item
            ? (string)item.Tag : "auto";

    private string GetSelectedTargetCode() =>
        TargetLanguageCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item
            ? (string)item.Tag : "zh-TW";

    private bool _llmAvailable;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        RestorePosition(settings);
        _llmAvailable = !string.IsNullOrEmpty(settings.ApiKey);
        UpdateEngineButtons(settings.Engine);
        ApplyDpiScaling();
    }

    private void ApplyDpiScaling()
    {
        var scale = Helpers.Win32Interop.GetSystemDpiScale();
        if (scale > 1.05) // Only apply if scaling > 105%
        {
            var transform = new ScaleTransform(scale, scale);
            MainBorder.LayoutTransform = transform;
            Width *= scale;
            Height *= scale;
            MinWidth = 520 * scale;
            MinHeight = 280 * scale;
        }
    }

    public void RefreshLlmAvailability()
    {
        var settings = _settingsService.Load();
        _llmAvailable = !string.IsNullOrEmpty(settings.ApiKey);
        UpdateEngineButtons(_translationService.CurrentEngineName);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ClipGeometry.Rect = new Rect(0, 0, MainBorder.ActualWidth, MainBorder.ActualHeight);
    }

    public void ShowWithTranslation(string text, string targetLanguage)
    {
        _currentSourceText = text;
        _currentTargetLanguage = targetLanguage;

        InputTextBox.Text = text;

        // Set source to "auto" and target to detected language
        SetSourceLanguageCombo("auto");
        SetTargetLanguageCombo(targetLanguage);

        Show();
        Activate();

        try
        {
            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);
        }
        catch { /* animation not critical */ }

        _ = TranslateAsync(text, targetLanguage);
    }

    private static void DebugLog(string msg)
    {
        try
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dt-debug.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
        catch { /* ignore logging errors */ }
    }

    private async Task TranslateAsync(string text, string targetLanguage)
    {
        DebugLog($"TranslateAsync called: text='{text.Substring(0, Math.Min(text.Length, 30))}' target={targetLanguage}");

        if (_isTranslating)
        {
            DebugLog("Already translating, skipping");
            return;
        }
        _isTranslating = true;

        try
        {
            DebugLog("ShowLoading...");
            ShowLoading(true);
            ErrorPanel.Visibility = Visibility.Collapsed;
            TranslationTextBox.Text = "";
            DebugLog($"Calling TranslationService (engine={_translationService.CurrentEngineName})...");
            var result = await _translationService.TranslateAsync(text, targetLanguage);
            DebugLog($"Result: success={result.IsSuccess}, text='{result.TranslatedText?.Substring(0, Math.Min(result.TranslatedText?.Length ?? 0, 50))}', error={result.ErrorMessage}");

            ShowLoading(false);
            _isTranslating = false;

            if (result.IsSuccess)
            {
                TranslationTextBox.Visibility = Visibility.Visible;
                TranslationTextBox.Text = result.TranslatedText;

                var resultFadeIn = (Storyboard)FindResource("ResultFadeInStoryboard");
                resultFadeIn.Begin(this);

                // Use local detector for display (Google's DetectedSourceLanguage is unreliable)
                var localDetected = LanguageDetector.DetectSourceLanguage(text);
                _currentSourceLanguage = localDetected != "unknown" ? localDetected : result.DetectedSourceLanguage;
                DebugLog($"GoogleDetected='{result.DetectedSourceLanguage}' LocalDetected='{localDetected}' Using='{_currentSourceLanguage}'");

                // Update source combo to show detected language (if auto mode)
                if (GetSelectedSourceCode() == "auto")
                {
                    var langName = GetLanguageName(_currentSourceLanguage);
                    if (SourceLanguageCombo.Items[0] is System.Windows.Controls.ComboBoxItem autoItem)
                    {
                        autoItem.Content = $"自動偵測 ({langName})";
                    }
                }

                _historyService.Add(new TranslationHistoryEntry(
                    text, result.TranslatedText,
                    result.DetectedSourceLanguage, targetLanguage,
                    _translationService.CurrentEngineName,
                    DateTime.UtcNow));
                UpdateHistoryLabel();
            }
            else
            {
                TranslationTextBox.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorText.Text = result.ErrorMessage ?? "翻譯失敗";
                DebugLog($"Translation FAILED: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ShowLoading(false);
            _isTranslating = false;
            DebugLog($"TranslateAsync EXCEPTION: {ex}");
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = "翻譯時發生錯誤";
        }
    }

    private Storyboard? _shimmerStoryboard;

    private void ShowLoading(bool show)
    {
        ShimmerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        TranslationTextBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;

        try
        {
            if (show)
            {
                // Use Begin() without containingObject to avoid NameScope lookup issues
                _shimmerStoryboard = (Storyboard)FindResource("ShimmerStoryboard");
                _shimmerStoryboard.Begin(ShimmerPanel, true);
            }
            else
            {
                _shimmerStoryboard?.Stop(ShimmerPanel);
                _shimmerStoryboard = null;
            }
        }
        catch (Exception ex)
        {
            // Don't let shimmer animation failure block translation
            DebugLog($"Shimmer animation error (non-fatal): {ex.Message}");
        }
    }

    // Title bar drag
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    // Engine switch
    private void EngineGoogle_Click(object sender, RoutedEventArgs e)
    {
        _translationService.SetEngine("google");
        UpdateEngineButtons("google");
        if (!string.IsNullOrEmpty(_currentSourceText))
            _ = TranslateAsync(_currentSourceText, _currentTargetLanguage);
    }

    private void EngineLlm_Click(object sender, RoutedEventArgs e)
    {
        if (!_llmAvailable)
        {
            System.Windows.MessageBox.Show(
                "請先在「設定」中輸入 API Key 才能使用 LLM 翻譯。\n\n右鍵系統匣圖示 → 設定",
                "LLM 未設定", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _translationService.SetEngine("llm");
        UpdateEngineButtons("llm");
        if (!string.IsNullOrEmpty(_currentSourceText))
            _ = TranslateAsync(_currentSourceText, _currentTargetLanguage);
    }

    private void UpdateEngineButtons(string engine)
    {
        // LLM button disabled state when no API key
        BtnLlm.IsEnabled = _llmAvailable;
        BtnLlm.Opacity = _llmAvailable ? 1.0 : 0.4;
        if (!_llmAvailable && engine == "llm")
            engine = "google"; // fallback

        if (engine == "google")
        {
            BtnGoogle.Background = (System.Windows.Media.Brush)FindResource("SegmentActiveBrush");
            BtnGoogle.Foreground = (System.Windows.Media.Brush)FindResource("SegmentActiveTextBrush");
            BtnLlm.Background = (System.Windows.Media.Brush)FindResource("SegmentInactiveBrush");
            BtnLlm.Foreground = (System.Windows.Media.Brush)FindResource("SegmentInactiveTextBrush");
        }
        else
        {
            BtnLlm.Background = (System.Windows.Media.Brush)FindResource("SegmentActiveBrush");
            BtnLlm.Foreground = (System.Windows.Media.Brush)FindResource("SegmentActiveTextBrush");
            BtnGoogle.Background = (System.Windows.Media.Brush)FindResource("SegmentInactiveBrush");
            BtnGoogle.Foreground = (System.Windows.Media.Brush)FindResource("SegmentInactiveTextBrush");
        }
    }

    // Pin toggle
    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        BtnPin.Opacity = Topmost ? 1.0 : 0.5;
    }

    // Minimize to tray
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }

    // Close to tray
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }

    public void HideWindow()
    {
        SavePosition();
        var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
        fadeOut.Begin(this);
    }

    private void FadeOut_Completed(object sender, EventArgs e)
    {
        Hide();
    }

    // TTS
    private void TtsSource_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(InputTextBox.Text))
        {
            var lang = _currentTargetLanguage == "en" ? "zh" : "en";
            _ttsService.Speak(InputTextBox.Text, lang);
        }
    }

    private void TtsTarget_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TranslationTextBox.Text))
        {
            _ttsService.Speak(TranslationTextBox.Text, _currentTargetLanguage);
        }
    }

    // Clear input
    private void ClearInput_Click(object sender, RoutedEventArgs e)
    {
        InputTextBox.Text = "";
        TranslationTextBox.Text = "";
        ErrorPanel.Visibility = Visibility.Collapsed;
        SetSourceLanguageCombo("auto");
        // Reset auto label
        if (SourceLanguageCombo.Items[0] is System.Windows.Controls.ComboBoxItem autoItem)
            autoItem.Content = "自動偵測";
    }

    // Copy translation result
    private void CopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TranslationTextBox.Text))
        {
            System.Windows.Clipboard.SetText(TranslationTextBox.Text);
        }
    }

    // Retry translation
    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSourceText))
            _ = TranslateAsync(_currentSourceText, _currentTargetLanguage);
    }

    // Swap source ↔ target languages and text
    private void SwapLanguages_Click(object sender, RoutedEventArgs e)
    {
        var translatedText = TranslationTextBox.Text;
        if (string.IsNullOrWhiteSpace(translatedText)) return;

        // Get current selections
        var oldSourceCode = GetSelectedSourceCode();
        var oldTargetCode = GetSelectedTargetCode();

        // If source was "auto", use the detected source language instead
        var actualSourceCode = oldSourceCode == "auto" ? _currentSourceLanguage : oldSourceCode;
        if (string.IsNullOrEmpty(actualSourceCode) || actualSourceCode == "unknown")
            actualSourceCode = "en";

        // Swap: old target → new source, old source → new target
        SetSourceLanguageCombo(oldTargetCode);
        SetTargetLanguageCombo(actualSourceCode);

        // Reset auto-detect label if it was showing
        if (SourceLanguageCombo.Items[0] is System.Windows.Controls.ComboBoxItem autoItem)
            autoItem.Content = "自動偵測";

        // Move translated text to input
        _suppressLanguageChangeEvent = true;
        InputTextBox.Text = translatedText;
        _suppressLanguageChangeEvent = false;

        // Translate with swapped settings
        _currentSourceText = translatedText;
        _currentTargetLanguage = actualSourceCode;
        _ = TranslateAsync(translatedText, actualSourceCode);
    }

    // Language combo selection changes
    private void SourceLanguageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChangeEvent) return;
        // Reset auto-detect label
        if (SourceLanguageCombo.Items[0] is System.Windows.Controls.ComboBoxItem autoItem)
            autoItem.Content = "自動偵測";

        RetranslateWithCurrentSettings();
    }

    private void TargetLanguageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChangeEvent) return;
        RetranslateWithCurrentSettings();
    }

    private void RetranslateWithCurrentSettings()
    {
        var text = InputTextBox.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        var sourceCode = GetSelectedSourceCode();
        var targetCode = GetSelectedTargetCode();

        // If source is auto, use LanguageDetector
        if (sourceCode == "auto")
        {
            targetCode = LanguageDetector.GetTargetLanguage(text);
            SetTargetLanguageCombo(targetCode);
        }

        _currentSourceText = text;
        _currentTargetLanguage = targetCode;
        _ = TranslateAsync(text, targetCode);
    }

    // Input debounce for re-translation on edit
    private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        var newText = InputTextBox.Text;
        if (!string.IsNullOrWhiteSpace(newText) && newText != _currentSourceText)
        {
            _currentSourceText = newText;

            var sourceCode = GetSelectedSourceCode();
            if (sourceCode == "auto")
            {
                _currentTargetLanguage = LanguageDetector.GetTargetLanguage(newText);
                SetTargetLanguageCombo(_currentTargetLanguage);
            }
            else
            {
                _currentTargetLanguage = GetSelectedTargetCode();
            }

            _ = TranslateAsync(newText, _currentTargetLanguage);
        }
    }

    // History panel
    private void ToggleHistory_Click(object sender, RoutedEventArgs e)
    {
        _historyExpanded = !_historyExpanded;
        HistoryListBox.Visibility = _historyExpanded ? Visibility.Visible : Visibility.Collapsed;
        HistoryArrow.Text = _historyExpanded ? "\u25BC" : "\u25B2";

        if (_historyExpanded)
        {
            HistoryListBox.ItemsSource = _historyService.GetAll().Reverse().ToList();
        }
    }

    private void UpdateHistoryLabel()
    {
        HistoryLabel.Text = $"歷史紀錄 ({_historyService.GetAll().Count} 筆)";
    }

    // Language name mapping
    private static string GetLanguageName(string code) => code.ToLower() switch
    {
        "zh" or "zh-cn" or "zh-tw" or "zh-hant" or "zh-hans" => "中文",
        "en" => "English",
        "ja" => "日本語",
        "ko" => "한국어",
        "fr" => "Français",
        "de" => "Deutsch",
        "es" => "Español",
        "pt" => "Português",
        "ru" => "Русский",
        "ar" => "العربية",
        "th" => "ไทย",
        "vi" => "Tiếng Việt",
        "hi" => "हिन्दी",
        "it" => "Italiano",
        "nl" => "Nederlands",
        "pl" => "Polski",
        "tr" => "Türkçe",
        "id" => "Bahasa Indonesia",
        "ms" => "Bahasa Melayu",
        "uk" => "Українська",
        "sv" => "Svenska",
        _ => code
    };

    // Position persistence
    private void RestorePosition(AppSettings settings)
    {
        Left = settings.WindowX;
        Top = settings.WindowY;
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
        Topmost = settings.AlwaysOnTop;
        BtnPin.Opacity = Topmost ? 1.0 : 0.5;
    }

    private void SavePosition()
    {
        var settings = _settingsService.Load();
        var updated = settings with
        {
            WindowX = Left,
            WindowY = Top,
            WindowWidth = Width,
            WindowHeight = Height,
            AlwaysOnTop = Topmost
        };
        _settingsService.Save(updated);
    }
}
