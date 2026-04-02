using Microsoft.Win32;

namespace DesktopTranslation.Helpers;

public static class ThemeHelper
{
    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool ShouldUseDarkTheme(string themeSetting)
    {
        return themeSetting switch
        {
            "dark" => true,
            "light" => false,
            _ => IsSystemDarkTheme()
        };
    }
}
