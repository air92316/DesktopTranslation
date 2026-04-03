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

    /// <summary>
    /// Detect the source language of the input text using character-range heuristics.
    /// Returns an ISO 639-1 code (e.g. "en", "ja", "ko", "zh-TW", "ar", "th", "ru").
    /// Falls back to "unknown" when detection is uncertain.
    /// </summary>
    public static string DetectSourceLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "unknown";

        int total = 0, cjk = 0, hiraganaKatakana = 0, hangul = 0, latin = 0;
        int arabic = 0, thai = 0, cyrillic = 0, devanagari = 0;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                continue;

            total++;

            if (IsCjk(c))
                cjk++;
            else if (IsHiraganaOrKatakana(c))
                hiraganaKatakana++;
            else if (IsHangul(c))
                hangul++;
            else if (IsLatin(c))
                latin++;
            else if (IsArabic(c))
                arabic++;
            else if (IsThai(c))
                thai++;
            else if (IsCyrillic(c))
                cyrillic++;
            else if (IsDevanagari(c))
                devanagari++;
        }

        if (total == 0)
            return "unknown";

        // Japanese: presence of hiragana/katakana is a strong signal
        if (hiraganaKatakana > 0 && (double)(hiraganaKatakana + cjk) / total > 0.3)
            return "ja";

        // Korean
        if ((double)hangul / total > 0.3)
            return "ko";

        // Chinese (CJK without Japanese kana)
        if ((double)cjk / total > 0.3)
            return "zh-TW";

        // Script-specific languages
        if ((double)arabic / total > 0.3)
            return "ar";
        if ((double)thai / total > 0.3)
            return "th";
        if ((double)cyrillic / total > 0.3)
            return "ru";
        if ((double)devanagari / total > 0.3)
            return "hi";

        // Latin-script languages default to English (most common case)
        if ((double)latin / total > 0.5)
            return "en";

        return "unknown";
    }

    private static bool IsCjk(char c)
    {
        return c >= '\u4E00' && c <= '\u9FFF'    // CJK Unified Ideographs
            || c >= '\u3400' && c <= '\u4DBF'    // CJK Extension A
            || c >= '\uF900' && c <= '\uFAFF';   // CJK Compatibility
    }

    private static bool IsHiraganaOrKatakana(char c)
    {
        return c >= '\u3040' && c <= '\u309F'    // Hiragana
            || c >= '\u30A0' && c <= '\u30FF';   // Katakana
    }

    private static bool IsHangul(char c)
    {
        return c >= '\uAC00' && c <= '\uD7AF'    // Hangul Syllables
            || c >= '\u1100' && c <= '\u11FF'    // Hangul Jamo
            || c >= '\u3130' && c <= '\u318F';   // Hangul Compatibility Jamo
    }

    private static bool IsLatin(char c)
    {
        return c >= 'A' && c <= 'Z'
            || c >= 'a' && c <= 'z'
            || c >= '\u00C0' && c <= '\u024F';   // Latin Extended
    }

    private static bool IsArabic(char c)
    {
        return c >= '\u0600' && c <= '\u06FF'
            || c >= '\u0750' && c <= '\u077F';
    }

    private static bool IsThai(char c)
    {
        return c >= '\u0E00' && c <= '\u0E7F';
    }

    private static bool IsCyrillic(char c)
    {
        return c >= '\u0400' && c <= '\u04FF';
    }

    private static bool IsDevanagari(char c)
    {
        return c >= '\u0900' && c <= '\u097F';
    }
}
