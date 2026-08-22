namespace Training.Module04.Core;

public sealed record Customer(string Email, string DisplayName);

/// <summary>
/// Collapses customer records that are the same person.
///
/// Exercise: the upstream system does not normalise email case, so treating
/// "ana@example.com" and "ANA@example.com" as different customers means two
/// invoices to the same person.
///
/// Unique must stay lazy. Taking one result must pull one item — an
/// implementation that builds a set of everything before yielding anything
/// passes every other test here and fails that one.
/// </summary>
public static class CustomerDeduplication
{
    public static IEnumerable<Customer> Unique(IEnumerable<Customer> customers)
        => throw new NotImplementedException();

    public static IReadOnlyDictionary<string, IReadOnlyList<Customer>> GroupByAddress(
        IEnumerable<Customer> customers)
        => throw new NotImplementedException();
}
