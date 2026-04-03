using System.Windows;
using System.Windows.Input;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Views;

public partial class UpdateNotificationWindow : Window
{
    private readonly UpdateService _updateService;
    private readonly UpdateInfo _updateInfo;
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _downloadCts;

    public UpdateNotificationWindow(
        UpdateService updateService,
        UpdateInfo updateInfo,
        SettingsService settingsService)
    {
        InitializeComponent();
        _updateService = updateService;
        _updateInfo = updateInfo;
        _settingsService = settingsService;
        PopulateInfo();
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

    private void PopulateInfo()
    {
        var currentVersion = typeof(App).Assembly.GetName().Version;
        TxtCurrentVersion.Text = $"v{currentVersion?.Major}.{currentVersion?.Minor}.{currentVersion?.Build}";
        TxtNewVersion.Text = _updateInfo.Version;
        TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
            ? "無更新說明"
            : _updateInfo.ReleaseNotes;
        TxtFileSize.Text = $"檔案大小：{FormatFileSize(_updateInfo.FileSizeBytes)}";
    }

    private static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        BtnUpdate.IsEnabled = false;
        BtnSkip.IsEnabled = false;
        BtnLater.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;

        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<double>(ratio =>
        {
            var percent = ratio * 100;
            DownloadProgress.Value = percent;
            TxtProgressPercent.Text = $"{percent:F0}%";
        });

        try
        {
            TxtProgressStatus.Text = "下載中...";
            var installerPath = await _updateService.DownloadUpdateAsync(
                _updateInfo, progress, _downloadCts.Token);

            TxtProgressStatus.Text = "啟動安裝程式...";
            _updateService.LaunchInstallerAndExit(installerPath);
        }
        catch (OperationCanceledException)
        {
            ResetButtons();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"下載更新失敗：{ex.Message}",
                "更新錯誤",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ResetButtons();
        }
    }

    private void ResetButtons()
    {
        ProgressPanel.Visibility = Visibility.Collapsed;
        DownloadProgress.Value = 0;
        TxtProgressPercent.Text = "0%";
        BtnUpdate.IsEnabled = true;
        BtnSkip.IsEnabled = true;
        BtnLater.IsEnabled = true;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        _settingsService.Save(settings with { SkippedVersion = _updateInfo.Version });
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _downloadCts?.Cancel();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        base.OnClosed(e);
    }
}
