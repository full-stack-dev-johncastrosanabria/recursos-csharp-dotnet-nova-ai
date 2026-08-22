namespace Training.Module04.Core;

/// <summary>
/// Revenue reporting over orders.
///
/// Exercise: only paid orders are revenue. Group by region, total each group,
/// and rank descending — then break ties by region name. Without an explicit
/// tie-break the order falls out of grouping order, which is an implementation
/// detail, and a report that reshuffles between runs on equal values looks
/// broken to whoever reads it.
/// </summary>
public static class LedgerAggregates
{
    public static IReadOnlyDictionary<string, decimal> RevenueByRegion(IEnumerable<Order> orders)
        => throw new NotImplementedException();

    public static IEnumerable<(string Region, decimal Revenue)> RegionsByRevenue(IEnumerable<Order> orders)
        => throw new NotImplementedException();
}
