namespace Training.Module04.Core;

public sealed record Customer(string Email, string DisplayName);

/// <summary>
/// Collapses customer records that are the same person.
///
/// DistinctBy keeps the first occurrence and streams: it holds only the set of
/// keys seen so far, and yields each item as it decides. Writing this as
/// "build a HashSet of everything, then emit" gives identical results and
/// destroys the laziness — which matters exactly when the source is large
/// enough for deduplication to have been worth doing.
///
/// The comparer is the actual bug fix. Ordinal comparison would treat
/// "ana@example.com" and "ANA@example.com" as two people, and the second
/// invoice is how you find out.
/// </summary>
public static class CustomerDeduplication
{
    public static IEnumerable<Customer> Unique(IEnumerable<Customer> customers)
        => customers.DistinctBy(customer => customer.Email, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, IReadOnlyList<Customer>> GroupByAddress(
        IEnumerable<Customer> customers)
        => customers
            .GroupBy(customer => customer.Email.ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Customer>)[.. group],
                StringComparer.Ordinal);
}
