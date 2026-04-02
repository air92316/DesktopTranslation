using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class DoubleTapDetectorTests
{
    [Fact]
    public void SingleTap_ReturnsFalse()
    {
        var detector = new DoubleTapDetector(intervalMs: 400);

        Assert.False(detector.RecordTap());
    }

    [Fact]
    public void TwoTapsWithinInterval_ReturnsTrue()
    {
        var detector = new DoubleTapDetector(intervalMs: 1000);

        // First tap
        Assert.False(detector.RecordTap());

        // Second tap within interval (we rely on the calls being < 1000ms apart)
        Thread.Sleep(80);
        Assert.True(detector.RecordTap());
    }

    [Fact]
    public void TwoTapsBeyondInterval_ReturnsFalse()
    {
        var detector = new DoubleTapDetector(intervalMs: 100);

        Assert.False(detector.RecordTap());

        // Wait longer than interval
        Thread.Sleep(150);
        Assert.False(detector.RecordTap());
    }

    [Fact]
    public void TwoTapsUnder50ms_ReturnsFalse_KeyRepeatFiltering()
    {
        // With a very large interval, taps under 50ms apart should still be rejected
        var detector = new DoubleTapDetector(intervalMs: 5000);

        Assert.False(detector.RecordTap());

        // Immediate second tap (< 50ms = key repeat territory)
        // No sleep at all to stay under 50ms
        Assert.False(detector.RecordTap());
    }

    [Fact]
    public void TripleTap_SecondTriggers_ThirdDoesNot()
    {
        var detector = new DoubleTapDetector(intervalMs: 1000);

        // 1st tap
        Assert.False(detector.RecordTap());

        // 2nd tap - triggers double tap, resets state
        Thread.Sleep(80);
        Assert.True(detector.RecordTap());

        // 3rd tap - starts fresh (previous was reset to MinValue), so no double tap
        Thread.Sleep(80);
        Assert.False(detector.RecordTap());
    }

    [Fact]
    public void UpdateInterval_ChangesThreshold()
    {
        var detector = new DoubleTapDetector(intervalMs: 100);

        Assert.False(detector.RecordTap());

        // Wait longer than original interval but shorter than updated one
        Thread.Sleep(150);

        // With original 100ms interval this would be false.
        // Update to 500ms so the 150ms gap still counts.
        detector.UpdateInterval(500);
        Assert.True(detector.RecordTap());
    }

    [Fact]
    public void AfterDoubleTap_StateResetsForNextSequence()
    {
        var detector = new DoubleTapDetector(intervalMs: 1000);

        // First double-tap sequence
        Assert.False(detector.RecordTap());
        Thread.Sleep(80);
        Assert.True(detector.RecordTap());

        // New sequence after reset
        Thread.Sleep(80);
        Assert.False(detector.RecordTap()); // fresh start
        Thread.Sleep(80);
        Assert.True(detector.RecordTap()); // second double-tap
    }
}
