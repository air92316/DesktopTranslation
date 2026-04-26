using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopTranslation.Models;
using DesktopTranslation.Services;
using DesktopTranslation.Services.Llm;

namespace DesktopTranslation.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly Action<AppSettings> _onSettingsApplied;
    private AppSettings? _loadedSnapshot;
    private AppSettings _workingSettings = new();
    private bool _allowClose;
    private bool _closePromptOpen;
    private bool _saveFeedbackInProgress;
    private bool _suppressProviderEvents;
    private bool _modelIsImplicitDefault;

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
        _workingSettings = settings;

        _suppressProviderEvents = true;
        try
        {
            RbGoogle.IsChecked = settings.Engine == "google";
            RbLlm.IsChecked = settings.Engine == "llm";

            CbProvider.SelectedIndex = settings.LlmProvider switch
            {
                "openai" => 1,
                "gemini" => 2,
                _ => 0,
            };

            ApplyProviderUi(settings.LlmProvider, settings);

            TxtBaseUrl.Text = settings.LlmBaseUrl;
            SliderTemperature.Value = settings.LlmTemperature;
            SliderMaxTokens.Value = settings.LlmMaxTokens;
            TxtTemperatureValue.Text = Math.Round(settings.LlmTemperature, 1).ToString("F1");
            TxtMaxTokensValue.Text = ((int)settings.LlmMaxTokens).ToString();

            ChkAutoStart.IsChecked = settings.AutoStart;
            ChkAutoUpdate.IsChecked = settings.AutoUpdateEnabled;
            SliderInterval.Value = settings.DoubleTapInterval;
            SliderSpeed.Value = settings.TtsSpeed;

            var themeIndex = settings.Theme switch
            {
                "light" => 1,
                "dark" => 2,
                _ => 0,
            };
            CbTheme.SelectedIndex = themeIndex;
        }
        finally
        {
            _suppressProviderEvents = false;
        }

        // snapshot after UI values are set; ReadCurrentSettings must see the same state
        var normalized = ReadCurrentSettings();
        _loadedSnapshot = normalized;
        _workingSettings = normalized;
    }

    private void ApplyProviderUi(string provider, AppSettings settings)
    {
        var rawModels = LlmModelCatalog.GetModels(provider);
        var defaultId = LlmModelCatalog.GetDefault(provider);
        var displayModels = BuildDisplayList(rawModels, defaultId);
        CbModel.ItemsSource = displayModels;

        var selectedModelId = settings.LlmModel ?? string.Empty;
        _modelIsImplicitDefault = false;

        if (displayModels.Count == 0)
        {
            CbModel.SelectedIndex = -1;
            TxtCustomModel.Visibility = Visibility.Collapsed;
            TxtCustomModel.Text = string.Empty;
        }
        else if (string.IsNullOrEmpty(selectedModelId))
        {
            // implicit default: keep settings.LlmModel == "" so engine can fallback
            CbModel.SelectedItem = displayModels[0];
            _modelIsImplicitDefault = true;
            TxtCustomModel.Visibility = Visibility.Collapsed;
            TxtCustomModel.Text = string.Empty;
        }
        else
        {
            LlmModelEntry? match = null;
            foreach (var entry in displayModels)
            {
                if (!entry.IsCustom && string.Equals(entry.Id, selectedModelId, StringComparison.Ordinal))
                {
                    match = entry;
                    break;
                }
            }

            if (match is not null)
            {
                CbModel.SelectedItem = match;
                TxtCustomModel.Visibility = Visibility.Collapsed;
                TxtCustomModel.Text = string.Empty;
            }
            else
            {
                LlmModelEntry? customEntry = null;
                foreach (var entry in displayModels)
                {
                    if (entry.IsCustom)
                    {
                        customEntry = entry;
                        break;
                    }
                }

                if (customEntry is not null)
                {
                    CbModel.SelectedItem = customEntry;
                    TxtCustomModel.Visibility = Visibility.Visible;
                    TxtCustomModel.Text = selectedModelId;
                }
                else
                {
                    CbModel.SelectedItem = displayModels[0];
                    _modelIsImplicitDefault = true;
                    TxtCustomModel.Visibility = Visibility.Collapsed;
                    TxtCustomModel.Text = string.Empty;
                }
            }
        }

        TxtApiKey.Password = SettingsService.GetEffectiveApiKey(settings, provider);
        PnlBaseUrl.Visibility = provider == "openai" ? Visibility.Visible : Visibility.Collapsed;
    }

    private static List<LlmModelEntry> BuildDisplayList(IReadOnlyList<LlmModelEntry> raw, string defaultId)
    {
        var list = new List<LlmModelEntry>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var entry = raw[i];
            if (i == 0 && !entry.IsCustom && !string.IsNullOrEmpty(defaultId))
            {
                list.Add(entry with { DisplayName = $"使用預設 ({defaultId})" });
            }
            else
            {
                list.Add(entry);
            }
        }
        return list;
    }

    private static string GetSelectedProvider(System.Windows.Controls.ComboBox provider)
    {
        var tag = (provider.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return tag switch
        {
            "openai" => "openai",
            "gemini" => "gemini",
            _ => "claude",
        };
    }

    private string GetSelectedModelId()
    {
        if (CbModel.SelectedItem is not LlmModelEntry entry)
            return string.Empty;

        if (entry.IsCustom)
            return TxtCustomModel.Text.Trim();

        // first non-custom item is the implicit-default slot; preserve "" in settings so engine can fallback
        if (_modelIsImplicitDefault)
            return string.Empty;

        return entry.Id;
    }

    private AppSettings ReadCurrentSettings()
    {
        // _workingSettings carries per-provider keys captured during provider switches.
        // _loadedSnapshot is the post-normalize baseline used by IsDirty comparison only.
        var baseline = _workingSettings;
        var themeTag = (CbTheme.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "system";
        var provider = GetSelectedProvider(CbProvider);
        var modelId = GetSelectedModelId();
        var apiKey = TxtApiKey.Password;

        var withApiKey = baseline with
        {
            ApiKey = apiKey,
            ClaudeApiKey = provider == "claude" ? apiKey : baseline.ClaudeApiKey,
            OpenAiApiKey = provider == "openai" ? apiKey : baseline.OpenAiApiKey,
            GeminiApiKey = provider == "gemini" ? apiKey : baseline.GeminiApiKey,
        };

        return withApiKey with
        {
            Engine = RbLlm.IsChecked == true ? "llm" : "google",
            LlmProvider = provider,
            LlmModel = modelId,
            LlmBaseUrl = TxtBaseUrl.Text.Trim(),
            LlmTemperature = Math.Round(SliderTemperature.Value, 1),
            LlmMaxTokens = (int)SliderMaxTokens.Value,
            AutoStart = ChkAutoStart.IsChecked == true,
            AutoUpdateEnabled = ChkAutoUpdate.IsChecked == true,
            DoubleTapInterval = (int)SliderInterval.Value,
            TtsSpeed = Math.Round(SliderSpeed.Value, 1),
            Theme = themeTag,
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

    private void CbProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressProviderEvents) return;
        if (_loadedSnapshot is null) return;

        // capture key user just typed for the previous provider before switching
        var previousProvider = _workingSettings.LlmProvider;
        var typedKey = TxtApiKey.Password;
        if (!string.IsNullOrEmpty(typedKey) && previousProvider is "claude" or "openai" or "gemini")
        {
            _workingSettings = previousProvider switch
            {
                "claude" => _workingSettings with { ClaudeApiKey = typedKey, ApiKey = typedKey },
                "openai" => _workingSettings with { OpenAiApiKey = typedKey, ApiKey = typedKey },
                "gemini" => _workingSettings with { GeminiApiKey = typedKey, ApiKey = typedKey },
                _ => _workingSettings,
            };
        }

        var provider = GetSelectedProvider(CbProvider);
        _workingSettings = _workingSettings with { LlmProvider = provider };

        _suppressProviderEvents = true;
        try
        {
            ApplyProviderUi(provider, _workingSettings);
        }
        finally
        {
            _suppressProviderEvents = false;
        }
    }

    private void CbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressProviderEvents) return;

        // any explicit user selection drops the implicit-default flag
        _modelIsImplicitDefault = false;

        if (CbModel.SelectedItem is LlmModelEntry entry && entry.IsCustom)
        {
            TxtCustomModel.Visibility = Visibility.Visible;
            TxtCustomModel.Focus();
        }
        else
        {
            TxtCustomModel.Visibility = Visibility.Collapsed;
        }
    }

    private void SliderTemperature_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtTemperatureValue != null)
            TxtTemperatureValue.Text = Math.Round(e.NewValue, 1).ToString("F1");
    }

    private void SliderMaxTokens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtMaxTokensValue != null)
            TxtMaxTokensValue.Text = ((int)e.NewValue).ToString();
    }

    private void ResetAdvanced_Click(object sender, RoutedEventArgs e)
    {
        SliderTemperature.Value = 0.3;
        SliderMaxTokens.Value = 2048;
    }
}
