namespace Training.Module01.Core;

/// <summary>
/// A reference to a customer, as the upstream system issues it.
///
/// The one rule that matters: whatever comparison Equals uses, GetHashCode must
/// use the same one. Region is compared case-insensitively, so the hash must be
/// computed case-insensitively too — otherwise "eu" and "EU" are equal but land
/// in different buckets, and the dictionary silently misses.
/// </summary>
public sealed class CustomerReference : IEquatable<CustomerReference>
{
    public CustomerReference(string region, int number)
    {
        Region = region;
        Number = number;
    }

    public string Region { get; }

    public int Number { get; }

    public bool Equals(CustomerReference? other)
        => other is not null
           && Number == other.Number
           && string.Equals(Region, other.Region, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as CustomerReference);

    public override int GetHashCode()
        => HashCode.Combine(Region.ToUpperInvariant(), Number);

    public static bool operator ==(CustomerReference? left, CustomerReference? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(CustomerReference? left, CustomerReference? right)
        => !(left == right);
}
