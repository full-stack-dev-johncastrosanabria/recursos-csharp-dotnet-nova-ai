namespace Training.Module02.Core;

public sealed record OrderSummary(string OrderId, decimal Total);

/// <summary>
/// A cache of recently seen orders that cannot grow without limit.
///
/// Exercise: this is the module's real-world case, as a repair. The version
/// that ships the bug is the same class without a capacity — a dictionary that
/// only ever gains keys. Nothing about it looks wrong, no test fails, and the
/// garbage collector cannot help because every entry is still reachable.
///
/// Hold at most Capacity entries and evict the least recently used. Reading an
/// entry counts as using it.
/// </summary>
public sealed class BoundedOrderCache
{
    public BoundedOrderCache(int capacity) => Capacity = capacity;

    public int Capacity { get; }

    public int Count => throw new NotImplementedException();

    public void Put(string orderId, OrderSummary order) => throw new NotImplementedException();

    public bool TryGet(string orderId, out OrderSummary? order) => throw new NotImplementedException();
}
