using DesktopTranslation.Helpers;

namespace DesktopTranslation.Tests.Helpers;

public class ThemeHelperTests
{
    [Fact]
    public void ShouldUseDarkTheme_DarkSetting_ReturnsTrue()
    {
        Assert.True(ThemeHelper.ShouldUseDarkTheme("dark"));
    }

    [Fact]
    public void ShouldUseDarkTheme_LightSetting_ReturnsFalse()
    {
        Assert.False(ThemeHelper.ShouldUseDarkTheme("light"));
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("DARK")]
    [InlineData("Light")]
    [InlineData("LIGHT")]
    public void ShouldUseDarkTheme_CaseSensitive_FallsToSystem(string setting)
    {
        // Non-exact matches fall through to the system theme default branch
        // This just verifies it doesn't throw for unexpected casing
        _ = ThemeHelper.ShouldUseDarkTheme(setting);
    }

    [Theory]
    [InlineData("system")]
    [InlineData("")]
    [InlineData("auto")]
    [InlineData("unknown")]
    public void ShouldUseDarkTheme_NonDarkNonLight_FallsToSystem(string setting)
    {
        // Should not throw; returns based on system theme
        _ = ThemeHelper.ShouldUseDarkTheme(setting);
    }

    [Fact]
    public void IsSystemDarkTheme_DoesNotThrow()
    {
        // Registry-based; just verify it runs without exception on any machine
        _ = ThemeHelper.IsSystemDarkTheme();
    }
}
