using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopTranslation.Helpers;
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

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        RestorePosition(settings);
        ApplyTheme(settings.Theme);
        UpdateEngineButtons(settings.Engine);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update clip geometry to match actual size
        ClipGeometry.Rect = new Rect(0, 0, MainBorder.ActualWidth, MainBorder.ActualHeight);
    }

    public void ShowWithTranslation(string text, string targetLanguage)
    {
        _currentSourceText = text;
        _currentTargetLanguage = targetLanguage;

        InputTextBox.Text = text;
        TargetLanguageLabel.Text = targetLanguage == "en" ? "English" : "繁體中文";

        var sourceLang = targetLanguage == "en" ? "中文 - 已偵測" : "已偵測";
        SourceLanguageLabel.Text = sourceLang;

        Show();
        Activate();

        var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
        fadeIn.Begin(this);

        _ = TranslateAsync(text, targetLanguage);
    }

    private async Task TranslateAsync(string text, string targetLanguage)
    {
        if (_isTranslating) return;
        _isTranslating = true;

        ShowLoading(true);
        ErrorPanel.Visibility = Visibility.Collapsed;
        TranslationTextBox.Text = "";

        var result = await _translationService.TranslateAsync(text, targetLanguage);

        ShowLoading(false);
        _isTranslating = false;

        if (result.IsSuccess)
        {
            TranslationTextBox.Visibility = Visibility.Visible;
            TranslationTextBox.Text = result.TranslatedText;

            var resultFadeIn = (Storyboard)FindResource("ResultFadeInStoryboard");
            resultFadeIn.Begin(this);

            _currentSourceLanguage = result.DetectedSourceLanguage;
            if (result.DetectedSourceLanguage != "unknown" && result.DetectedSourceLanguage != "auto")
            {
                SourceLanguageLabel.Text = $"{result.DetectedSourceLanguage} - 已偵測";
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
        }
    }

    private void ShowLoading(bool show)
    {
        ShimmerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        TranslationTextBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;

        if (show)
        {
            var shimmer = (Storyboard)FindResource("ShimmerStoryboard");
            shimmer.Begin(this, true);
        }
        else
        {
            var shimmer = (Storyboard)FindResource("ShimmerStoryboard");
            shimmer.Stop(this);
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
        _translationService.SetEngine("llm");
        UpdateEngineButtons("llm");
        if (!string.IsNullOrEmpty(_currentSourceText))
            _ = TranslateAsync(_currentSourceText, _currentTargetLanguage);
    }

    private void UpdateEngineButtons(string engine)
    {
        if (engine == "google")
        {
            BtnGoogle.Background = (Brush)FindResource("SegmentActiveBrush");
            BtnGoogle.Foreground = (Brush)FindResource("SegmentActiveTextBrush");
            BtnLlm.Background = (Brush)FindResource("SegmentInactiveBrush");
            BtnLlm.Foreground = (Brush)FindResource("SegmentInactiveTextBrush");
        }
        else
        {
            BtnLlm.Background = (Brush)FindResource("SegmentActiveBrush");
            BtnLlm.Foreground = (Brush)FindResource("SegmentActiveTextBrush");
            BtnGoogle.Background = (Brush)FindResource("SegmentInactiveBrush");
            BtnGoogle.Foreground = (Brush)FindResource("SegmentInactiveTextBrush");
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
        SourceLanguageLabel.Text = "偵測中...";
    }

    // Copy translation result
    private void CopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TranslationTextBox.Text))
        {
            Clipboard.SetText(TranslationTextBox.Text);
        }
    }

    // Retry translation
    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSourceText))
            _ = TranslateAsync(_currentSourceText, _currentTargetLanguage);
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
            _currentTargetLanguage = LanguageDetector.GetTargetLanguage(newText);
            TargetLanguageLabel.Text = _currentTargetLanguage == "en" ? "English" : "繁體中文";
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

    // Theme
    public void ApplyTheme(string themeSetting)
    {
        var isDark = ThemeHelper.ShouldUseDarkTheme(themeSetting);

        Resources["BgBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x2D, 0x2D, 0x2D) : Color.FromRgb(0xFF, 0xFF, 0xFF));
        Resources["TextBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0xE5, 0xE5, 0xE5) : Color.FromRgb(0x1A, 0x1A, 0x1A));
        Resources["SeparatorBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x40, 0x40, 0x40) : Color.FromRgb(0xE5, 0xE5, 0xE5));
        Resources["AccentBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x60, 0xCD, 0xFF) : Color.FromRgb(0x00, 0x78, 0xD4));
        Resources["LabelBrush"] = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        Resources["TitleBarBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x25, 0x25, 0x25) : Color.FromRgb(0xF3, 0xF3, 0xF3));
        Resources["ButtonHoverBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x3A, 0x3A, 0x3A) : Color.FromRgb(0xE8, 0xE8, 0xE8));
        Resources["SegmentActiveBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x60, 0xCD, 0xFF) : Color.FromRgb(0x00, 0x78, 0xD4));
        Resources["SegmentInactiveBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x40, 0x40, 0x40) : Color.FromRgb(0xE0, 0xE0, 0xE0));
        Resources["SegmentActiveTextBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x1A, 0x1A, 0x1A) : Color.FromRgb(0xFF, 0xFF, 0xFF));
        Resources["SegmentInactiveTextBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0xA0, 0xA0, 0xA0) : Color.FromRgb(0x55, 0x55, 0x55));
        Resources["ShimmerBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x3A, 0x3A, 0x3A) : Color.FromRgb(0xE8, 0xE8, 0xE8));
        Resources["ErrorBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0xEF, 0x53, 0x50) : Color.FromRgb(0xD3, 0x2F, 0x2F));
        Resources["HistoryBgBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0x25, 0x25, 0x25) : Color.FromRgb(0xFA, 0xFA, 0xFA));

        // Re-apply engine button colors after theme change
        var currentEngine = _translationService.CurrentEngineName == "Google Translate" ? "google" : "llm";
        UpdateEngineButtons(currentEngine);
    }
}
