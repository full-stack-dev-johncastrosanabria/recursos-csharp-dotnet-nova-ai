namespace Training.Module01.Core;

public readonly record struct LineItem(string Sku, int Quantity);

/// <summary>
/// The key an idempotency cache uses to recognise a retried checkout.
///
/// The compiler-generated record equality compares Lines by reference, because
/// that is what Equals does on IReadOnlyList&lt;T&gt;. Two identical baskets
/// then hash differently, the cache misses on retry, and the charge repeats.
/// SequenceEqual and a matching hash fix it.
/// </summary>
public sealed record BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)
{
    public bool Equals(BasketKey? other)
        => other is not null
           && string.Equals(CustomerId, other.CustomerId, StringComparison.Ordinal)
           && Lines.SequenceEqual(other.Lines);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CustomerId, StringComparer.Ordinal);

        foreach (var line in Lines)
        {
            hash.Add(line);
        }

        return hash.ToHashCode();
    }
}
