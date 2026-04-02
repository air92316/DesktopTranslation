using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class HistoryServiceTests
{
    [Fact]
    public void Add_StoresEntry()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "hello", "你好", "en", "zh-TW", "google", DateTime.UtcNow));

        Assert.Single(service.GetAll());
    }

    [Fact]
    public void Add_BeyondMax_RemovesOldest()
    {
        var service = new HistoryService(maxEntries: 3);

        for (int i = 0; i < 5; i++)
            service.Add(new TranslationHistoryEntry(
                $"text{i}", $"translated{i}", "en", "zh-TW", "google", DateTime.UtcNow));

        var all = service.GetAll();
        Assert.Equal(3, all.Count);
        Assert.Equal("text2", all[0].SourceText);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "hello", "你好", "en", "zh-TW", "google", DateTime.UtcNow));

        service.Clear();
        Assert.Empty(service.GetAll());
    }
}
