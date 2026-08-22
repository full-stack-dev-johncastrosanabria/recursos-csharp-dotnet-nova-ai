using Shouldly;
using Training.Module04.Core;

namespace Training.Module04.Tests.Core;

public sealed class LedgerAggregatesTests
{
    private static readonly Order[] Orders =
    [
        new("ord_1", "EU", 120m, OrderState.Paid),
        new("ord_2", "EU", 40m, OrderState.Pending),
        new("ord_3", "US", 300m, OrderState.Paid),
        new("ord_4", "US", 15m, OrderState.Cancelled),
        new("ord_5", "EU", 220m, OrderState.Paid),
    ];

    [Fact]
    public void Totals_revenue_by_region()
    {
        var byRegion = LedgerAggregates.RevenueByRegion(Orders);

        byRegion["EU"].ShouldBe(340m);
        byRegion["US"].ShouldBe(300m);
    }

    [Fact]
    public void Every_region_present_in_the_source_appears()
    {
        LedgerAggregates.RevenueByRegion(Orders).Keys.Order().ShouldBe(["EU", "US"]);
    }

    [Fact]
    public void An_empty_source_produces_an_empty_report()
    {
        LedgerAggregates.RevenueByRegion([]).ShouldBeEmpty();
    }

    [Fact]
    public void Ranks_regions_by_revenue_descending()
    {
        var ranked = LedgerAggregates.RegionsByRevenue(Orders).ToArray();

        ranked.ShouldBe([("EU", 340m), ("US", 300m)]);
    }

    [Fact]
    public void Ties_are_broken_by_region_name_so_the_order_is_stable()
    {
        // Without an explicit tie-break the order depends on grouping order,
        // which is an implementation detail. A report that reshuffles between
        // runs on equal values looks broken to whoever reads it.
        Order[] tied =
        [
            new("a", "ZZ", 100m, OrderState.Paid),
            new("b", "AA", 100m, OrderState.Paid),
        ];

        LedgerAggregates.RegionsByRevenue(tied).Select(r => r.Region).ShouldBe(["AA", "ZZ"]);
    }

    [Fact]
    public void Only_paid_orders_count_as_revenue()
    {
        Order[] mixed =
        [
            new("a", "EU", 100m, OrderState.Pending),
            new("b", "EU", 50m, OrderState.Cancelled),
        ];

        LedgerAggregates.RevenueByRegion(mixed).ShouldBeEmpty();
    }
}
