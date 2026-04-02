namespace DesktopTranslation.Services;

public class DoubleTapDetector
{
    private DateTime _lastTapTime = DateTime.MinValue;
    private int _intervalMs;

    public DoubleTapDetector(int intervalMs = 400)
    {
        _intervalMs = intervalMs;
    }

    public void UpdateInterval(int intervalMs)
    {
        _intervalMs = intervalMs;
    }

    /// <summary>
    /// Records a tap event. Returns true if a double-tap was detected.
    /// Ignores taps within 50ms of each other (key repeat filtering).
    /// </summary>
    public bool RecordTap()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastTapTime).TotalMilliseconds;

        if (elapsed < _intervalMs && elapsed > 50)
        {
            _lastTapTime = DateTime.MinValue;
            return true;
        }

        _lastTapTime = now;
        return false;
    }
}
