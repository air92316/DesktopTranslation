using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

/// <summary>
/// In-memory LRU cache of successful translations, keyed by engine + target language + source text.
/// Prevents re-hitting rate-limited free endpoints for text the user already translated
/// (retry, swap back, re-selecting a language, re-opening the same clipboard content).
/// </summary>
public sealed class TranslationCache
{
    private sealed record CacheEntry(string Key, TranslationResult Result);

    private const char KeySeparator = '\u001F'; // ASCII unit separator: never appears in language codes

    private readonly int _capacity;
    private readonly object _lock = new();
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _index = new(StringComparer.Ordinal);
    private readonly LinkedList<CacheEntry> _order = new(); // head = most recently used

    public TranslationCache(int capacity = 200)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        _capacity = capacity;
    }

    public int Count
    {
        get
        {
            lock (_lock)
                return _index.Count;
        }
    }

    public bool TryGet(string engine, string targetLanguage, string text, out TranslationResult result)
    {
        var key = BuildKey(engine, targetLanguage, text);
        lock (_lock)
        {
            if (_index.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                result = node.Value.Result;
                return true;
            }
        }

        result = null!;
        return false;
    }

    /// <summary>Stores a result. Failed results are ignored so transient errors never get replayed.</summary>
    public void Set(string engine, string targetLanguage, string text, TranslationResult result)
    {
        if (!result.IsSuccess)
            return;

        var key = BuildKey(engine, targetLanguage, text);
        lock (_lock)
        {
            if (_index.TryGetValue(key, out var existing))
            {
                _order.Remove(existing);
                _index.Remove(key);
            }

            var node = new LinkedListNode<CacheEntry>(new CacheEntry(key, result));
            _order.AddFirst(node);
            _index[key] = node;

            while (_index.Count > _capacity && _order.Last is { } oldest)
            {
                _order.RemoveLast();
                _index.Remove(oldest.Value.Key);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _index.Clear();
            _order.Clear();
        }
    }

    private static string BuildKey(string engine, string targetLanguage, string text) =>
        string.Concat(engine, KeySeparator, targetLanguage, KeySeparator, text);
}
