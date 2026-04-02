using System.Diagnostics;
using System.Windows;

namespace DesktopTranslation.Services;

public class ClipboardService
{
    private const int MaxTextLength = 10_000;

    public string? GetText()
    {
        try
        {
            if (!Clipboard.ContainsText())
                return null;

            var text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return null;

            return text.Length > MaxTextLength ? text[..MaxTextLength] : text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Clipboard access error: {ex.Message}");
            return null;
        }
    }
}
