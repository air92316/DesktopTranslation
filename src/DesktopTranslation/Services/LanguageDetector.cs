namespace DesktopTranslation.Services;

public static class LanguageDetector
{
    private const double CjkThreshold = 0.30;

    public static string GetTargetLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "zh-TW";

        var totalChars = 0;
        var cjkChars = 0;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                continue;

            totalChars++;
            if (IsCjk(c))
                cjkChars++;
        }

        if (totalChars == 0)
            return "zh-TW";

        var ratio = (double)cjkChars / totalChars;
        return ratio > CjkThreshold ? "en" : "zh-TW";
    }

    private static bool IsCjk(char c)
    {
        return c >= '\u4E00' && c <= '\u9FFF'    // CJK Unified Ideographs
            || c >= '\u3400' && c <= '\u4DBF'    // CJK Extension A
            || c >= '\uF900' && c <= '\uFAFF';   // CJK Compatibility
    }
}
