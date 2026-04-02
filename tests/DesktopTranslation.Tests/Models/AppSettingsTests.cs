using DesktopTranslation.Models;

namespace DesktopTranslation.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Default_Values_Are_Correct()
    {
        var settings = new AppSettings();

        Assert.Equal(720, settings.WindowWidth);
        Assert.Equal(400, settings.WindowHeight);
        Assert.True(settings.AlwaysOnTop);
        Assert.Equal("google", settings.Engine);
        Assert.Equal("claude", settings.LlmProvider);
        Assert.Equal("", settings.ApiKey);
        Assert.False(settings.AutoStart);
        Assert.Equal(400, settings.DoubleTapInterval);
        Assert.Equal(1.0, settings.TtsSpeed);
        Assert.Equal("system", settings.Theme);
    }

    [Fact]
    public void Clone_Creates_Independent_Copy()
    {
        var original = new AppSettings { Engine = "google" };
        var clone = original with { Engine = "llm" };

        Assert.Equal("google", original.Engine);
        Assert.Equal("llm", clone.Engine);
    }
}
