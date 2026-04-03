using System.Diagnostics;
using System.Runtime.InteropServices;
using DesktopTranslation.Helpers;

namespace DesktopTranslation.Services;

public class HotkeyService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly Win32Interop.LowLevelKeyboardProc _hookProc;
    private readonly DoubleTapDetector _doubleTapDetector;
    private bool _ctrlPressed;

    public event Action? DoubleCopyDetected;

    public HotkeyService(int doubleTapInterval = 400)
    {
        _doubleTapDetector = new DoubleTapDetector(doubleTapInterval);
        _hookProc = HookCallback;
    }

    public void UpdateInterval(int interval) => _doubleTapDetector.UpdateInterval(interval);

    public void Start()
    {
        // For .NET 8 WPF, pass IntPtr.Zero as hMod for WH_KEYBOARD_LL
        // (low-level hooks don't need a module handle)
        _hookId = Win32Interop.SetWindowsHookEx(
            Win32Interop.WH_KEYBOARD_LL,
            _hookProc,
            IntPtr.Zero,
            0);

        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dt-debug.log");
        if (_hookId == IntPtr.Zero)
        {
            var err = Marshal.GetLastWin32Error();
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss}] HOOK FAILED! Error code: {err}\n");
        }
        else
        {
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss}] HOOK INSTALLED OK. Handle: {_hookId}\n");
        }
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private static bool IsCtrlKey(uint vkCode) =>
        vkCode == Win32Interop.VK_CONTROL
        || vkCode == Win32Interop.VK_LCONTROL
        || vkCode == Win32Interop.VK_RCONTROL;

    private static int _logCounter;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Interop.KBDLLHOOKSTRUCT>(lParam);
            var isKeyDown = wParam == (IntPtr)Win32Interop.WM_KEYDOWN
                         || wParam == (IntPtr)Win32Interop.WM_SYSKEYDOWN;
            var isKeyUp = wParam == (IntPtr)0x0101 || wParam == (IntPtr)0x0105; // WM_KEYUP / WM_SYSKEYUP

            // Log first 20 key events to debug
            if (_logCounter < 20 && isKeyDown)
            {
                _logCounter++;
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dt-debug.log");
                System.IO.File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss}] KEY vk=0x{hookStruct.vkCode:X2} ctrl={_ctrlPressed}\n");
            }

            if (IsCtrlKey(hookStruct.vkCode))
            {
                _ctrlPressed = isKeyDown;
            }
            else if (hookStruct.vkCode == Win32Interop.VK_C && isKeyDown && _ctrlPressed)
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dt-debug.log");
                System.IO.File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss}] CTRL+C detected\n");

                if (_doubleTapDetector.RecordTap())
                {
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:HH:mm:ss}] DOUBLE CTRL+C DETECTED!\n");

                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                        DoubleCopyDetected?.Invoke());
                }
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
