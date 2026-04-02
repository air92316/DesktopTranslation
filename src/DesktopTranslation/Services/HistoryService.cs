using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class HistoryService
{
    private readonly List<TranslationHistoryEntry> _entries = new();
    private readonly int _maxEntries;

    public HistoryService(int maxEntries = 50)
    {
        _maxEntries = maxEntries;
    }

    public IReadOnlyList<TranslationHistoryEntry> GetAll() => _entries.AsReadOnly();

    public void Add(TranslationHistoryEntry entry)
    {
        _entries.Add(entry);
        while (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);
    }

    public void Clear() => _entries.Clear();
}
