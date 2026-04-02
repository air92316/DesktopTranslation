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
}
