namespace Training.Module01.Core;

/// <summary>
/// A reference to a customer, as the upstream system issues it: a region code
/// and a number.
///
/// Exercise: implement the full equality contract. Region comparison is
/// case-insensitive, because the upstream system is inconsistent about it — and
/// that single fact is what makes GetHashCode interesting. Satisfy Equals
/// without satisfying GetHashCode and the dictionary tests will tell you.
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

    public bool Equals(CustomerReference? other) => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();

    public static bool operator ==(CustomerReference? left, CustomerReference? right)
        => throw new NotImplementedException();

    public static bool operator !=(CustomerReference? left, CustomerReference? right)
        => throw new NotImplementedException();
}
