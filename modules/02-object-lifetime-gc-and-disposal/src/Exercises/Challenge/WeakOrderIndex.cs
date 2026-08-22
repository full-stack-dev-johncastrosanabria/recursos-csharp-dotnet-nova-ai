namespace Training.Module02.Challenge;

/// <summary>An order document held elsewhere. The index must not keep it alive.</summary>
public sealed class OrderDocument(string orderId)
{
    public string OrderId { get; } = orderId;
}

/// <summary>
/// A lookup that lets its entries be collected.
///
/// Challenge: back it with WeakReference&lt;T&gt; so the index never keeps a
/// document alive on its own. Two things follow, and the second is the one
/// people miss: entries can die between adding and reading, so TryGet must
/// handle a reference whose target is gone; and the dictionary entry holding
/// the dead reference does not remove itself, so an index like this leaks its
/// own bookkeeping until something prunes it.
/// </summary>
public sealed class WeakOrderIndex
{
    public int Count => throw new NotImplementedException();

    public void Add(string orderId, OrderDocument document) => throw new NotImplementedException();

    public bool TryGet(string orderId, out OrderDocument? document) => throw new NotImplementedException();

    public int Prune() => throw new NotImplementedException();
}
