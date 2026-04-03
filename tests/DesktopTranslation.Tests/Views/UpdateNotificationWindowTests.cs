using DesktopTranslation.Views;

namespace DesktopTranslation.Tests.Views;

public class UpdateNotificationWindowTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    public void FormatFileSize_ByteRange(long bytes, string expected)
    {
        Assert.Equal(expected, UpdateNotificationWindow.FormatFileSize(bytes));
    }

    [Theory]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048575, "1024.0 KB")]
    public void FormatFileSize_KBRange(long bytes, string expected)
    {
        Assert.Equal(expected, UpdateNotificationWindow.FormatFileSize(bytes));
    }

    [Theory]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(5242880, "5.0 MB")]
    [InlineData(10485760, "10.0 MB")]
    public void FormatFileSize_MBRange(long bytes, string expected)
    {
        Assert.Equal(expected, UpdateNotificationWindow.FormatFileSize(bytes));
    }
}
