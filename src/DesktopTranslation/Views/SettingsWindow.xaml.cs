using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly Action<AppSettings> _onSettingsApplied;
    private AppSettings? _loadedSnapshot;
    private bool _allowClose;
    private bool _closePromptOpen;
    private bool _saveFeedbackInProgress;

    public SettingsWindow(SettingsService settingsService, Action<AppSettings> onSettingsApplied)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _onSettingsApplied = onSettingsApplied;
        LoadSettings();
        Loaded += (_, _) => ApplyDpiScaling();
    }

    private void ApplyDpiScaling()
    {
        var scale = Helpers.Win32Interop.GetSystemDpiScale();
        if (scale > 1.05)
        {
            var transform = new System.Windows.Media.ScaleTransform(scale, scale);
            if (Content is FrameworkElement root)
                root.LayoutTransform = transform;
            Width *= scale;
            Height *= scale;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        RbGoogle.IsChecked = settings.Engine == "google";
        RbLlm.IsChecked = settings.Engine == "llm";

        CbProvider.SelectedIndex = settings.LlmProvider == "openai" ? 1 : 0;
        TxtApiKey.Password = settings.ApiKey;
        ChkAutoStart.IsChecked = settings.AutoStart;
        ChkAutoUpdate.IsChecked = settings.AutoUpdateEnabled;
        SliderInterval.Value = settings.DoubleTapInterval;
        SliderSpeed.Value = settings.TtsSpeed;

        var themeIndex = settings.Theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };
        CbTheme.SelectedIndex = themeIndex;

        // snapshot after UI values are set; ReadCurrentSettings must see the same state
        _loadedSnapshot = ReadCurrentSettings();
    }

    private AppSettings ReadCurrentSettings()
    {
        var baseline = _loadedSnapshot ?? _settingsService.Load();
        var themeTag = (CbTheme.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "system";

        return baseline with
        {
            Engine = RbLlm.IsChecked == true ? "llm" : "google",
            LlmProvider = CbProvider.SelectedIndex == 1 ? "openai" : "claude",
            ApiKey = TxtApiKey.Password,
            AutoStart = ChkAutoStart.IsChecked == true,
            AutoUpdateEnabled = ChkAutoUpdate.IsChecked == true,
            DoubleTapInterval = (int)SliderInterval.Value,
            TtsSpeed = Math.Round(SliderSpeed.Value, 1),
            Theme = themeTag
        };
    }

    private bool IsDirty()
    {
        if (_loadedSnapshot is null) return false;
        return ReadCurrentSettings() != _loadedSnapshot;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SaveAndCloseAsync();
    }

    private async Task SaveAndCloseAsync()
    {
        var updated = ReadCurrentSettings();

        _settingsService.Save(updated);
        AutoStartService.SetEnabled(updated.AutoStart);
        _onSettingsApplied(updated);
        _loadedSnapshot = updated;

        await ShowSaveSuccessAsync();

        _allowClose = true;
        Close();
    }

    private async Task ShowSaveSuccessAsync()
    {
        _saveFeedbackInProgress = true;

        var originalContent = BtnSave.Content;
        var originalBackground = BtnSave.Background;
        var originalForeground = BtnSave.Foreground;
        BtnSave.IsEnabled = false;

        try
        {
            var successBrush =
                TryFindResource("SuccessBrush") as System.Windows.Media.Brush ??
                System.Windows.Media.Brushes.SeaGreen;
            var textInverseBrush =
                TryFindResource("TextInverseBrush") as System.Windows.Media.Brush ??
                System.Windows.Media.Brushes.White;

            BtnSave.Content = "\u2713 已儲存";
            BtnSave.Background = successBrush;
            BtnSave.Foreground = textInverseBrush;

            await Task.Delay(1500);
        }
        finally
        {
            BtnSave.Content = originalContent;
            BtnSave.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
            BtnSave.ClearValue(System.Windows.Controls.Button.ForegroundProperty);
            BtnSave.ClearValue(UIElement.IsEnabledProperty);
            _saveFeedbackInProgress = false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        // Allow programmatic close after Save success or explicit discard
        if (_allowClose)
        {
            base.OnClosing(e);
            return;
        }

        // Block close while the save-success animation is still running
        if (_saveFeedbackInProgress)
        {
            e.Cancel = true;
            return;
        }

        // Prevent MessageBox re-entry while a dirty prompt is already open
        if (_closePromptOpen)
        {
            e.Cancel = true;
            return;
        }

        if (!IsDirty())
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _closePromptOpen = true;

        MessageBoxResult result;
        try
        {
            result = System.Windows.MessageBox.Show(
                this,
                "設定已變更但尚未儲存。\n\n是：儲存並關閉\n否：放棄變更並關閉\n取消：返回設定視窗",
                "尚未儲存的設定",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);
        }
        finally
        {
            _closePromptOpen = false;
        }

        switch (result)
        {
            case MessageBoxResult.Yes:
                await SaveAndCloseAsync();
                break;
            case MessageBoxResult.No:
                _allowClose = true;
                Close();
                break;
            // Cancel: stay open, e.Cancel already set true
        }
    }

    private void SliderInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtIntervalValue != null)
            TxtIntervalValue.Text = ((int)e.NewValue).ToString();
    }

    private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtSpeedValue != null)
            TxtSpeedValue.Text = Math.Round(e.NewValue, 1).ToString("F1");
    }
}
