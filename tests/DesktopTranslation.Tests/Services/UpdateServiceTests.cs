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

    // --- IsNewerVersion null/edge cases ---

    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("1.0.0", null)]
    [InlineData(null, null)]
    public void IsNewerVersion_ReturnsFalse_WhenInputIsNull(string? remote, string? current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote!, current!));
    }

    [Theory]
    [InlineData(" ", "1.0.0")]
    [InlineData("1.0.0", " ")]
    [InlineData("  ", "  ")]
    public void IsNewerVersion_ReturnsFalse_WhenInputIsWhitespace(string remote, string current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote, current));
    }

    [Theory]
    [InlineData("1.2.3.4", "1.2.3.0", true)]
    [InlineData("1.2.3.0", "1.2.3.4", false)]
    [InlineData("1.2.3.4", "1.2.3.4", false)]
    public void IsNewerVersion_HandlesFourPartVersions(string remote, string current, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsNewerVersion(remote, current));
    }

    [Fact]
    public void IsNewerVersion_HandlesLargeVersionNumbers()
    {
        Assert.True(UpdateService.IsNewerVersion("999.999.999", "0.0.1"));
        Assert.False(UpdateService.IsNewerVersion("0.0.1", "999.999.999"));
    }

    [Theory]
    [InlineData("v", "1.0.0")]
    [InlineData("vv1.0.0", "1.0.0")]
    public void IsNewerVersion_ReturnsFalse_WhenPrefixOnlyOrDouble(string remote, string current)
    {
        Assert.False(UpdateService.IsNewerVersion(remote, current));
    }

    // --- IsValidDownloadUrl tests ---

    [Theory]
    [InlineData("https://github.com/user/repo/releases/download/v1.0/file.exe", true)]
    [InlineData("https://objects.githubusercontent.com/path/file.exe", true)]
    [InlineData("https://codeload.github.com/user/repo/archive.zip", true)]
    public void IsValidDownloadUrl_AcceptsGitHubDomains(string url, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsValidDownloadUrl(url));
    }

    [Theory]
    [InlineData("https://evil.com/malware.exe")]
    [InlineData("https://github.com.evil.com/fake.exe")]
    [InlineData("https://notgithub.com/file.exe")]
    public void IsValidDownloadUrl_RejectsNonGitHubDomains(string url)
    {
        Assert.False(UpdateService.IsValidDownloadUrl(url));
    }

    [Theory]
    [InlineData("http://github.com/file.exe")]
    [InlineData("ftp://github.com/file.exe")]
    public void IsValidDownloadUrl_RejectsNonHttpsSchemes(string url)
    {
        Assert.False(UpdateService.IsValidDownloadUrl(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("://missing-scheme")]
    public void IsValidDownloadUrl_RejectsInvalidUrls(string url)
    {
        Assert.False(UpdateService.IsValidDownloadUrl(url));
    }
}
