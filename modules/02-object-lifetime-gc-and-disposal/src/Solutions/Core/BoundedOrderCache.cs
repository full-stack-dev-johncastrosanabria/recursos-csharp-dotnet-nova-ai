namespace Training.Module02.Core;

public sealed record OrderSummary(string OrderId, decimal Total);

/// <summary>
/// A cache of recently seen orders that cannot grow without limit.
///
/// The dictionary answers "where is this entry" in constant time; the linked
/// list answers "which entry is coldest" in constant time. Neither structure
/// can do both, which is why an LRU cache needs the pair.
///
/// The bug this repairs is not a missing feature. It is a cache written without
/// deciding what happens when it fills up, which means deciding that it never
/// does.
/// </summary>
public sealed class BoundedOrderCache
{
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries;
    private readonly LinkedList<Entry> _recency = new();

    public BoundedOrderCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
        _entries = new Dictionary<string, LinkedListNode<Entry>>(StringComparer.Ordinal);
    }

    public int Capacity { get; }

    public int Count => _entries.Count;

    public void Put(string orderId, OrderSummary order)
    {
        if (_entries.TryGetValue(orderId, out var existing))
        {
            existing.Value = new Entry(orderId, order);
            Touch(existing);
            return;
        }

        if (_entries.Count >= Capacity)
        {
            var coldest = _recency.Last!;
            _recency.RemoveLast();
            _entries.Remove(coldest.Value.Key);
        }

        _entries[orderId] = _recency.AddFirst(new Entry(orderId, order));
    }

    public bool TryGet(string orderId, out OrderSummary? order)
    {
        if (!_entries.TryGetValue(orderId, out var node))
        {
            order = null;
            return false;
        }

        Touch(node);
        order = node.Value.Value;
        return true;
    }

    private void Touch(LinkedListNode<Entry> node)
    {
        _recency.Remove(node);
        _recency.AddFirst(node);
    }

    private readonly record struct Entry(string Key, OrderSummary Value);
}
