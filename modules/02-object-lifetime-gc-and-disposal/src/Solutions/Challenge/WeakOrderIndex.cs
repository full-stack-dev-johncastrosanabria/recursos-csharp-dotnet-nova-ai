namespace Training.Module02.Challenge;

/// <summary>An order document held elsewhere. The index must not keep it alive.</summary>
public sealed class OrderDocument(string orderId)
{
    public string OrderId { get; } = orderId;
}

/// <summary>
/// A lookup that lets its entries be collected.
///
/// Note what a weak reference does and does not buy. The document can be
/// collected, which is the point. The dictionary entry holding the dead
/// reference cannot collect itself, so the index still grows one small entry
/// per key forever unless something prunes it. A weak reference is a smaller
/// leak, not the absence of one.
/// </summary>
public sealed class WeakOrderIndex
{
    private readonly Dictionary<string, WeakReference<OrderDocument>> _entries =
        new(StringComparer.Ordinal);

    public int Count => _entries.Count;

    public void Add(string orderId, OrderDocument document)
        => _entries[orderId] = new WeakReference<OrderDocument>(document);

    public bool TryGet(string orderId, out OrderDocument? document)
    {
        if (_entries.TryGetValue(orderId, out var reference))
        {
            return reference.TryGetTarget(out document);
        }

        document = null;
        return false;
    }

    public int Prune()
    {
        var dead = _entries
            .Where(entry => !entry.Value.TryGetTarget(out _))
            .Select(entry => entry.Key)
            .ToList();

        foreach (var key in dead)
        {
            _entries.Remove(key);
        }

        return dead.Count;
    }
}
