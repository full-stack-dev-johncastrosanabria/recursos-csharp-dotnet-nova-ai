namespace Training.Module01.Core;

public readonly record struct LineItem(string Sku, int Quantity);

/// <summary>
/// The key an idempotency cache uses to recognise a retried checkout.
///
/// Exercise: a record gives you value equality for free, and here that free
/// equality is wrong — Lines is a reference, so two structurally identical
/// baskets compare unequal, the cache misses, and the customer is charged
/// twice. Override both members so structurally equal baskets are equal.
/// Whatever Equals compares, GetHashCode must agree with.
/// </summary>
public sealed record BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)
{
    public bool Equals(BasketKey? other) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();
}
