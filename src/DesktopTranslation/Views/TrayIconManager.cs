using System.Drawing;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace DesktopTranslation.Views;

public class TrayIconManager : IDisposable
{
    private readonly WinForms.NotifyIcon _trayIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onOpenSettings;
    private readonly Action<string> _onSwitchEngine;
    private readonly Action<bool> _onToggleAutoStart;
    private readonly Action _onCheckUpdate;
    private readonly Action _onExit;

    public TrayIconManager(
        Action onShowWindow,
        Action onOpenSettings,
        Action<string> onSwitchEngine,
        Action<bool> onToggleAutoStart,
        Action onCheckUpdate,
        Action onExit,
        bool autoStartEnabled,
        string currentEngine)
    {
        _onShowWindow = onShowWindow;
        _onOpenSettings = onOpenSettings;
        _onSwitchEngine = onSwitchEngine;
        _onToggleAutoStart = onToggleAutoStart;
        _onCheckUpdate = onCheckUpdate;
        _onExit = onExit;

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "DesktopTranslation — 雙擊 Ctrl+C 翻譯",
            Visible = true,
            ContextMenuStrip = BuildContextMenu(autoStartEnabled, currentEngine)
        };

        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                _onShowWindow();
        };
    }

    public void UpdateMenu(bool autoStartEnabled, string currentEngine)
    {
        _trayIcon.ContextMenuStrip = BuildContextMenu(autoStartEnabled, currentEngine);
    }

    private WinForms.ContextMenuStrip BuildContextMenu(bool autoStartEnabled, string currentEngine)
    {
        var menu = new WinForms.ContextMenuStrip();

        menu.Items.Add("顯示主視窗", null, (_, _) => _onShowWindow());
        menu.Items.Add(new WinForms.ToolStripSeparator());

        var googleItem = new WinForms.ToolStripMenuItem("Google")
        {
            Checked = currentEngine == "google"
        };
        googleItem.Click += (_, _) => _onSwitchEngine("google");

        var llmItem = new WinForms.ToolStripMenuItem("LLM")
        {
            Checked = currentEngine == "llm"
        };
        llmItem.Click += (_, _) => _onSwitchEngine("llm");

        var engineMenu = new WinForms.ToolStripMenuItem("翻譯引擎");
        engineMenu.DropDownItems.Add(googleItem);
        engineMenu.DropDownItems.Add(llmItem);
        menu.Items.Add(engineMenu);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        var autoStartItem = new WinForms.ToolStripMenuItem("開機自啟動")
        {
            Checked = autoStartEnabled
        };
        autoStartItem.Click += (_, _) => _onToggleAutoStart(!autoStartEnabled);
        menu.Items.Add(autoStartItem);

        menu.Items.Add("設定", null, (_, _) => _onOpenSettings());
        menu.Items.Add("檢查更新", null, (_, _) => _onCheckUpdate());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("關於", null, (_, _) =>
        {
            var version = typeof(App).Assembly.GetName().Version;
            var versionStr = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
            System.Windows.MessageBox.Show(
                $"DesktopTranslation {versionStr}\n" +
                "─────────────────────────\n" +
                "桌面即時翻譯工具\n\n" +
                "雙擊 Ctrl+C 即可將選取文字快速翻譯。\n" +
                "支援 Google Translate 及 LLM（Claude / OpenAI）引擎。\n" +
                "自動偵測語言方向，中文翻英文、其他語言翻中文。\n\n" +
                "開發者：Ramen Cat Studio\n" +
                "授權條款：MIT License\n" +
                "技術棧：C# / .NET 8 / WPF\n\n" +
                "© 2026 Ramen Cat Studio. All rights reserved.",
                "關於 DesktopTranslation", MessageBoxButton.OK, MessageBoxImage.Information);
        });
        menu.Items.Add("結束", null, (_, _) => _onExit());

        return menu;
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(74, 111, 165)); // #4A6FA5 Ai-iro
        using var font = new Font(new System.Drawing.FontFamily("Segoe UI"), 18, System.Drawing.FontStyle.Bold);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("T", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);

        var handle = bmp.GetHicon();
        return Icon.FromHandle(handle);
    }

    public void Dispose()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
