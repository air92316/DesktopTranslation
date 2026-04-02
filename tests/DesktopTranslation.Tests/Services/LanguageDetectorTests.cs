using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class LanguageDetectorTests
{
    [Theory]
    [InlineData("Hello world", "zh-TW")]
    [InlineData("This is a test", "zh-TW")]
    [InlineData("Bonjour le monde", "zh-TW")]
    [InlineData("これはテストです", "zh-TW")]  // Japanese -> still targets zh-TW
    public void Detect_NonChinese_ReturnsZhTW(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("你好世界", "en")]
    [InlineData("這是一個測試文字", "en")]
    [InlineData("今天天氣很好，我想出去走走", "en")]
    public void Detect_Chinese_ReturnsEn(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("Hello 你好 world", "zh-TW")]  // Mixed, CJK < 30%
    [InlineData("這是test測試", "en")]           // Mixed, CJK > 30%
    public void Detect_Mixed_UsesThreshold(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetTargetLanguage(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_EmptyOrWhitespace_DefaultsToZhTW(string input)
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage(input));
    }

    [Fact]
    public void Detect_OnlyPunctuation_DefaultsToZhTW()
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("...!!!???"));
    }

    [Fact]
    public void Detect_SingleCjkCharacter_ReturnsEn()
    {
        Assert.Equal("en", LanguageDetector.GetTargetLanguage("中"));
    }

    [Fact]
    public void Detect_SingleLatinCharacter_ReturnsZhTW()
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("A"));
    }

    [Fact]
    public void Detect_CjkExtensionA_ReturnsEn()
    {
        // \u3400 is first char in CJK Extension A
        Assert.Equal("en", LanguageDetector.GetTargetLanguage("\u3400\u3401\u3402"));
    }

    [Fact]
    public void Detect_OnlyDigits_DefaultsToZhTW()
    {
        // Digits are not CJK and not whitespace/punctuation, so they count as non-CJK chars
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("12345"));
    }

    [Theory]
    [InlineData("@#$%^&*")]
    [InlineData("+-=<>")]
    public void Detect_SymbolsOnly_DefaultsToZhTW(string input)
    {
        // Symbols that are classified as punctuation are skipped, leaving totalChars == 0
        // Symbols not classified as punctuation count as non-CJK
        var result = LanguageDetector.GetTargetLanguage(input);
        Assert.Equal("zh-TW", result);
    }

    [Fact]
    public void Detect_Emoji_DefaultsToZhTW()
    {
        // Emoji are outside CJK ranges and not punctuation/whitespace
        // They count as non-CJK characters
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("\uD83D\uDE00\uD83D\uDE01"));
    }

    [Fact]
    public void Detect_CjkCompatibility_ReturnsEn()
    {
        // \uF900 is first char in CJK Compatibility Ideographs
        Assert.Equal("en", LanguageDetector.GetTargetLanguage("\uF900\uF901\uF902"));
    }

    [Fact]
    public void Detect_MixedDigitsAndCjk_UsesThreshold()
    {
        // "123中" => totalChars=4, cjkChars=1, ratio=0.25 < 0.30 => zh-TW
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("123\u4E2D"));
    }

    [Fact]
    public void Detect_ExactlyAtThreshold_ReturnsZhTW()
    {
        // 3 CJK out of 10 total = 0.30 exactly; threshold is > 0.30, so this should be zh-TW
        // "abcdefg中中中" => 7 non-CJK + 3 CJK = 10, ratio = 0.30
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("abcdefg\u4E2D\u4E2D\u4E2D"));
    }

    [Fact]
    public void Detect_JustAboveThreshold_ReturnsEn()
    {
        // 4 CJK out of 10 total = 0.40 > 0.30 => en
        // "abcdef中中中中" => 6 non-CJK + 4 CJK = 10, ratio = 0.40
        Assert.Equal("en", LanguageDetector.GetTargetLanguage("abcdef\u4E2D\u4E2D\u4E2D\u4E2D"));
    }

    [Fact]
    public void Detect_Null_DefaultsToZhTW()
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage(null!));
    }

    [Fact]
    public void Detect_TabsAndNewlines_DefaultsToZhTW()
    {
        Assert.Equal("zh-TW", LanguageDetector.GetTargetLanguage("\t\n\r"));
    }
}
