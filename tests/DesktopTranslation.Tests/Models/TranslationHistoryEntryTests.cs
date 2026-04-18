using DesktopTranslation.Models;

namespace DesktopTranslation.Tests.Models;

public class TranslationHistoryEntryTests
{
    // ---------------------------------------------------------------------
    // TimestampLocal
    // ---------------------------------------------------------------------

    [Fact]
    public void TimestampLocal_UtcKind_ConvertsToLocal()
    {
        var utc = new DateTime(2026, 4, 18, 12, 34, 56, DateTimeKind.Utc);
        var entry = Make(timestamp: utc);

        Assert.Equal(utc.ToLocalTime(), entry.TimestampLocal);
        Assert.Equal(DateTimeKind.Local, entry.TimestampLocal.Kind);
    }

    [Fact]
    public void TimestampLocal_LocalKind_ReturnsAsIs()
    {
        var local = new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Local);
        var entry = Make(timestamp: local);

        Assert.Equal(local, entry.TimestampLocal);
    }

    [Fact]
    public void TimestampLocal_UnspecifiedKind_ReturnsAsIs()
    {
        // Unspecified shouldn't be silently converted — contract preserves non-UTC inputs verbatim.
        var unspec = new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Unspecified);
        var entry = Make(timestamp: unspec);

        Assert.Equal(unspec, entry.TimestampLocal);
        Assert.Equal(DateTimeKind.Unspecified, entry.TimestampLocal.Kind);
    }

    // ---------------------------------------------------------------------
    // TimestampDisplay
    // ---------------------------------------------------------------------

    [Fact]
    public void TimestampDisplay_SameDay_ShowsHourMinuteOnly()
    {
        // Use local "now" so TimestampLocal.Date == DateTime.Today regardless of host TZ.
        var today = DateTime.Now;
        var entry = Make(timestamp: DateTime.SpecifyKind(today, DateTimeKind.Local));

        var expected = today.ToString("HH:mm");
        Assert.Equal(expected, entry.TimestampDisplay);
    }

    [Fact]
    public void TimestampDisplay_DifferentDay_IncludesDate()
    {
        var pastLocal = DateTime.Today.AddDays(-5).AddHours(15).AddMinutes(42);
        var entry = Make(timestamp: DateTime.SpecifyKind(pastLocal, DateTimeKind.Local));

        Assert.Equal(pastLocal.ToString("MM/dd HH:mm"), entry.TimestampDisplay);
    }

    // ---------------------------------------------------------------------
    // EngineDisplay
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("google", "Google")]
    [InlineData("llm", "LLM")]
    public void EngineDisplay_KnownValues_FormattedForHumans(string input, string expected)
    {
        var entry = Make(engine: input);
        Assert.Equal(expected, entry.EngineDisplay);
    }

    [Fact]
    public void EngineDisplay_UnknownValue_TitleCasesFirstChar()
    {
        var entry = Make(engine: "deepl");
        Assert.Equal("Deepl", entry.EngineDisplay);
    }

    [Fact]
    public void EngineDisplay_EmptyString_ReturnsEmpty()
    {
        var entry = Make(engine: "");
        Assert.Equal("", entry.EngineDisplay);
    }

    // ---------------------------------------------------------------------
    // Record equality — computed properties must NOT participate
    // ---------------------------------------------------------------------

    [Fact]
    public void Equality_IgnoresComputedProperties()
    {
        // Two records with identical ctor params should be equal even though
        // TimestampDisplay could differ across day boundaries.
        var ts = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var a = Make(timestamp: ts);
        var b = Make(timestamp: ts);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static TranslationHistoryEntry Make(
        string sourceText = "hello",
        string translatedText = "你好",
        string sourceLanguage = "en",
        string targetLanguage = "zh-TW",
        string engine = "google",
        DateTime? timestamp = null)
        => new(
            sourceText,
            translatedText,
            sourceLanguage,
            targetLanguage,
            engine,
            timestamp ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}
