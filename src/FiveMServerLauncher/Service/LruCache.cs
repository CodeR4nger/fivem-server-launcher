namespace FiveMServerLauncher.Service;

// Least-recently-used bounded cache. Not thread-safe: callers synchronize access
// (the enrichment service under its gate, the icon converter on the UI thread).
public sealed class LruCache<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly LinkedList<TKey> _order = new();
    private readonly Dictionary<TKey, (TValue Value, LinkedListNode<TKey> Node)> _entries = new();

    public bool TryGet(TKey key, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            _order.Remove(entry.Node);
            _order.AddFirst(entry.Node);
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            _order.Remove(entry.Node);
            _order.AddFirst(entry.Node);
            _entries[key] = (value, entry.Node);
            return;
        }

        var node = _order.AddFirst(key);
        _entries[key] = (value, node);

        if (_entries.Count > capacity)
        {
            var evicted = _order.Last!;
            _order.RemoveLast();
            _entries.Remove(evicted.Value);
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        if (TryGet(key, out var value))
        {
            return value;
        }

        value = factory(key);
        Set(key, value);
        return value;
    }
}
