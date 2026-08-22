namespace Training.Module04.Core;

/// <summary>
/// Revenue reporting over orders.
///
/// The tie-break on region name is the part worth noticing. OrderByDescending
/// alone is stable in LINQ-to-Objects but the input order it preserves is
/// grouping order, which is an implementation detail nobody promised. Naming
/// the second key makes the report reproducible instead of merely usually
/// consistent.
/// </summary>
public static class LedgerAggregates
{
    public static IReadOnlyDictionary<string, decimal> RevenueByRegion(IEnumerable<Order> orders)
        => orders
            .Where(order => order.State == OrderState.Paid)
            .GroupBy(order => order.Region, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(o => o.Amount), StringComparer.Ordinal);

    public static IEnumerable<(string Region, decimal Revenue)> RegionsByRevenue(IEnumerable<Order> orders)
        => RevenueByRegion(orders)
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => (entry.Key, entry.Value));
}
