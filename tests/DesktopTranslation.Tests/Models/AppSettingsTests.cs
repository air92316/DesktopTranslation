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
    public void Default_Update_Settings_Are_Correct()
    {
        var settings = new AppSettings();

        Assert.True(settings.AutoUpdateEnabled);
        Assert.Equal(24, settings.UpdateCheckIntervalHours);
        Assert.Equal("", settings.SkippedVersion);
        Assert.Equal(DateTime.MinValue, settings.LastUpdateCheck);
    }

    [Fact]
    public void Clone_Creates_Independent_Copy()
    {
        var original = new AppSettings { Engine = "google" };
        var clone = original with { Engine = "llm" };

        Assert.Equal("google", original.Engine);
        Assert.Equal("llm", clone.Engine);
    }

    [Fact]
    public void WindowPosition_Defaults()
    {
        var settings = new AppSettings();
        Assert.Equal(100, settings.WindowX);
        Assert.Equal(200, settings.WindowY);
    }

    [Fact]
    public void WithExpression_PreservesOtherProperties()
    {
        var original = new AppSettings { Engine = "llm", Theme = "dark", ApiKey = "key123" };
        var modified = original with { TtsSpeed = 1.5 };

        Assert.Equal("llm", modified.Engine);
        Assert.Equal("dark", modified.Theme);
        Assert.Equal("key123", modified.ApiKey);
        Assert.Equal(1.5, modified.TtsSpeed);
    }
}
