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

    [Fact]
    public void GetAll_EmptyByDefault()
    {
        var service = new HistoryService();
        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Add_ExactlyAtMax_DoesNotRemove()
    {
        var service = new HistoryService(maxEntries: 2);
        service.Add(new TranslationHistoryEntry(
            "text0", "t0", "en", "zh-TW", "google", DateTime.UtcNow));
        service.Add(new TranslationHistoryEntry(
            "text1", "t1", "en", "zh-TW", "google", DateTime.UtcNow));

        Assert.Equal(2, service.GetAll().Count);
        Assert.Equal("text0", service.GetAll()[0].SourceText);
    }

    [Fact]
    public void Add_MaxEntriesOne_KeepsOnlyLatest()
    {
        var service = new HistoryService(maxEntries: 1);
        service.Add(new TranslationHistoryEntry(
            "first", "f", "en", "zh-TW", "google", DateTime.UtcNow));
        service.Add(new TranslationHistoryEntry(
            "second", "s", "en", "zh-TW", "google", DateTime.UtcNow));

        Assert.Single(service.GetAll());
        Assert.Equal("second", service.GetAll()[0].SourceText);
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyCopy()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "hello", "你好", "en", "zh-TW", "google", DateTime.UtcNow));

        var list = service.GetAll();
        Assert.IsAssignableFrom<IReadOnlyList<TranslationHistoryEntry>>(list);
    }

    [Fact]
    public void Add_NullEntry_ThrowsOrStores()
    {
        // TranslationHistoryEntry is a record; null is a valid reference
        var service = new HistoryService(maxEntries: 50);
        service.Add(null!);
        Assert.Single(service.GetAll());
        Assert.Null(service.GetAll()[0]);
    }

    [Fact]
    public void Add_EntryWithNullFields_Stores()
    {
        var service = new HistoryService(maxEntries: 50);
        var entry = new TranslationHistoryEntry(null!, null!, null!, null!, null!, DateTime.MinValue);
        service.Add(entry);

        var stored = service.GetAll()[0];
        Assert.Null(stored.SourceText);
        Assert.Null(stored.TranslatedText);
    }

    [Fact]
    public void Add_ManyEntries_MaintainsOrder()
    {
        var service = new HistoryService(maxEntries: 100);
        for (int i = 0; i < 10; i++)
            service.Add(new TranslationHistoryEntry(
                $"text{i}", $"t{i}", "en", "zh-TW", "google", DateTime.UtcNow));

        var all = service.GetAll();
        for (int i = 0; i < 10; i++)
            Assert.Equal($"text{i}", all[i].SourceText);
    }

    [Fact]
    public void Clear_ThenAdd_WorksNormally()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry(
            "first", "f", "en", "zh-TW", "google", DateTime.UtcNow));
        service.Clear();
        service.Add(new TranslationHistoryEntry(
            "second", "s", "en", "zh-TW", "google", DateTime.UtcNow));

        Assert.Single(service.GetAll());
        Assert.Equal("second", service.GetAll()[0].SourceText);
    }

    [Fact]
    public void Add_EmptyStrings_Stores()
    {
        var service = new HistoryService(maxEntries: 50);
        service.Add(new TranslationHistoryEntry("", "", "", "", "", DateTime.UtcNow));

        Assert.Single(service.GetAll());
        Assert.Equal("", service.GetAll()[0].SourceText);
    }
}
