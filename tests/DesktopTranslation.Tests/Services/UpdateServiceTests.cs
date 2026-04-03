using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.2.0", "1.1.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.1.1", "1.1.0", true)]
    public void IsNewerVersion_ReturnsTrue_WhenRemoteIsNewer(
        string remote, string current, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsNewerVersion(remote, current));
    }

    [Theory]
    [InlineData("1.0.0", "1.1.0")]
    [InlineData("0.9.0", "1.0.0")]
    [InlineData("1.0.0", "2.0.0")]
    public void IsNewerVersion_ReturnsFalse_WhenRemoteIsOlder(
        string remote, string current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote, current));
    }

    [Theory]
    [InlineData("1.1.0", "1.1.0")]
    [InlineData("2.0.0", "2.0.0")]
    public void IsNewerVersion_ReturnsFalse_WhenVersionsAreEqual(
        string remote, string current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote, current));
    }

    [Theory]
    [InlineData("invalid", "1.0.0")]
    [InlineData("1.0.0", "invalid")]
    [InlineData("", "1.0.0")]
    [InlineData("abc", "xyz")]
    public void IsNewerVersion_ReturnsFalse_WhenVersionFormatIsInvalid(
        string remote, string current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote, current));
    }

    [Theory]
    [InlineData("v1.2.0", "v1.1.0", true)]
    [InlineData("V1.2.0", "v1.1.0", true)]
    [InlineData("v1.0.0", "v1.0.0", false)]
    public void IsNewerVersion_HandlesVersionPrefix(
        string remote, string current, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsNewerVersion(remote, current));
    }
}
