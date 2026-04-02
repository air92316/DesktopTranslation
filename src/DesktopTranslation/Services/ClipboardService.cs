using System.Windows;

namespace DesktopTranslation.Services;

public class ClipboardService
{
    public string? GetText()
    {
        try
        {
            if (Clipboard.ContainsText())
                return Clipboard.GetText();
            return null;
        }
        catch
        {
            return null;
        }
    }
}
