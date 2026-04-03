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
        SliderInterval.Value = settings.DoubleTapInterval;
        SliderSpeed.Value = settings.TtsSpeed;

        var themeIndex = settings.Theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };
        CbTheme.SelectedIndex = themeIndex;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var themeTag = (CbTheme.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "system";

        var updated = settings with
        {
            Engine = RbLlm.IsChecked == true ? "llm" : "google",
            LlmProvider = CbProvider.SelectedIndex == 1 ? "openai" : "claude",
            ApiKey = TxtApiKey.Password,
            AutoStart = ChkAutoStart.IsChecked == true,
            DoubleTapInterval = (int)SliderInterval.Value,
            TtsSpeed = Math.Round(SliderSpeed.Value, 1),
            Theme = themeTag
        };

        _settingsService.Save(updated);
        AutoStartService.SetEnabled(updated.AutoStart);
        _onSettingsApplied(updated);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
