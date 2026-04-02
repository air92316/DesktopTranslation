using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;

namespace DesktopTranslation.Views;

public class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onOpenSettings;
    private readonly Action<string> _onSwitchEngine;
    private readonly Action<bool> _onToggleAutoStart;
    private readonly Action _onExit;

    public TrayIconManager(
        Action onShowWindow,
        Action onOpenSettings,
        Action<string> onSwitchEngine,
        Action<bool> onToggleAutoStart,
        Action onExit,
        bool autoStartEnabled,
        string currentEngine)
    {
        _onShowWindow = onShowWindow;
        _onOpenSettings = onOpenSettings;
        _onSwitchEngine = onSwitchEngine;
        _onToggleAutoStart = onToggleAutoStart;
        _onExit = onExit;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "DesktopTranslation",
            ContextMenu = BuildContextMenu(autoStartEnabled, currentEngine)
        };

        _trayIcon.TrayLeftMouseDown += (_, _) => _onShowWindow();
    }

    public void UpdateMenu(bool autoStartEnabled, string currentEngine)
    {
        _trayIcon.ContextMenu = BuildContextMenu(autoStartEnabled, currentEngine);
    }

    private ContextMenu BuildContextMenu(bool autoStartEnabled, string currentEngine)
    {
        var menu = new ContextMenu();

        var showItem = new MenuItem { Header = "顯示主視窗" };
        showItem.Click += (_, _) => _onShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new Separator());

        var engineMenu = new MenuItem { Header = "翻譯引擎" };
        var googleItem = new MenuItem
        {
            Header = "Google",
            IsCheckable = true,
            IsChecked = currentEngine == "google"
        };
        googleItem.Click += (_, _) => _onSwitchEngine("google");
        var llmItem = new MenuItem
        {
            Header = "LLM",
            IsCheckable = true,
            IsChecked = currentEngine == "llm"
        };
        llmItem.Click += (_, _) => _onSwitchEngine("llm");
        engineMenu.Items.Add(googleItem);
        engineMenu.Items.Add(llmItem);
        menu.Items.Add(engineMenu);

        menu.Items.Add(new Separator());

        var autoStartItem = new MenuItem
        {
            Header = "開機自啟動",
            IsCheckable = true,
            IsChecked = autoStartEnabled
        };
        autoStartItem.Click += (_, _) => _onToggleAutoStart(!autoStartEnabled);
        menu.Items.Add(autoStartItem);

        var settingsItem = new MenuItem { Header = "設定" };
        settingsItem.Click += (_, _) => _onOpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = "關於" };
        aboutItem.Click += (_, _) => MessageBox.Show(
            "DesktopTranslation v1.0\n雙擊 Ctrl+C 快速翻譯",
            "關於", MessageBoxButton.OK, MessageBoxImage.Information);
        menu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = "結束" };
        exitItem.Click += (_, _) => _onExit();
        menu.Items.Add(exitItem);

        return menu;
    }

    public void Dispose()
    {
        _trayIcon.Dispose();
    }
}
