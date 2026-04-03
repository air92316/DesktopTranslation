using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
        TxtNewVersion.Text = _updateInfo.Version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? _updateInfo.Version
            : $"v{_updateInfo.Version}";
        TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
            ? "無更新說明"
            : StripMarkdown(_updateInfo.ReleaseNotes);
        TxtFileSize.Text = $"檔案大小：{FormatFileSize(_updateInfo.FileSizeBytes)}";
    }

    internal static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }

    private static string StripMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return markdown;

        var lines = markdown.Split('\n');
        var result = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            // Remove heading markers
            if (trimmed.StartsWith('#'))
                trimmed = trimmed.TrimStart('#').TrimStart();
            // Convert bullet lists
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                trimmed = "・" + trimmed[2..];
            // Remove bold markers
            trimmed = trimmed.Replace("**", "");
            // Remove inline code markers
            trimmed = trimmed.Replace("`", "");

            result.AppendLine(trimmed);
        }

        return result.ToString().TrimEnd();
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

            // Check disk space
            var tempDrive = new DriveInfo(Path.GetPathRoot(Path.GetTempPath())!);
            var requiredBytes = _updateInfo.FileSizeBytes + (100 * 1024 * 1024); // file + 100MB safety margin
            if (tempDrive.AvailableFreeSpace < requiredBytes)
            {
                System.Windows.MessageBox.Show(
                    $"磁碟空間不足，需要至少 {FormatFileSize(requiredBytes)} 的可用空間。",
                    "更新錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ResetButtons();
                return;
            }

            var installerPath = await _updateService.DownloadUpdateAsync(
                _updateInfo, progress, _downloadCts.Token);

            TxtProgressStatus.Text = "啟動安裝程式...";
            _updateService.LaunchInstallerAndExit(installerPath);
        }
        catch (OperationCanceledException)
        {
            TxtProgressStatus.Text = "已取消下載";
            ResetButtons();
        }
        catch (Exception ex)
        {
            var userMessage = ex switch
            {
                HttpRequestException => "無法連接到更新伺服器，請檢查網路連線。",
                TaskCanceledException => "連線逾時，請稍後再試。",
                InvalidOperationException when ex.Message.Contains("file size") =>
                    "下載的檔案不完整，請稍後再試。",
                _ => "下載更新時發生錯誤，請稍後再試。"
            };
            Debug.WriteLine($"[UPDATE] Download failed: {ex}");
            System.Windows.MessageBox.Show(
                userMessage,
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

    private void CancelDownload_Click(object sender, RoutedEventArgs e)
    {
        _downloadCts?.Cancel();
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
