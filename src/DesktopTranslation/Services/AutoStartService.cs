using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopTranslation.Services;

public static class AutoStartService
{
    private const string AppName = "DesktopTranslation";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AutoStart check failed: {ex.Message}");
            return false;
        }
    }

    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    Debug.WriteLine("AutoStart: ProcessPath is null, cannot register auto-start.");
                    return false;
                }

                key.SetValue(AppName, $"\"{exePath}\"");
                Debug.WriteLine($"AutoStart: Registered at \"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Debug.WriteLine("AutoStart: Registry entry removed");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AutoStart set failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ensures the registry entry matches the current exe path.
    /// Call on startup when settings indicate auto-start is enabled.
    /// </summary>
    public static void SyncRegistry(bool settingsEnabled)
    {
        if (!settingsEnabled) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            var currentValue = key?.GetValue(AppName) as string;
            var expectedValue = $"\"{Environment.ProcessPath}\"";

            if (currentValue != expectedValue)
            {
                Debug.WriteLine($"AutoStart: Registry out of sync (was: {currentValue ?? "null"}, expected: {expectedValue}). Re-registering...");
                SetEnabled(true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AutoStart sync failed: {ex.Message}");
        }
    }
}
