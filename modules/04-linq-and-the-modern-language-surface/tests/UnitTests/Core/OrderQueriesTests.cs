using Shouldly;
using Training.Module04.Core;

namespace Training.Module04.Tests.Core;

public sealed class OrderQueriesTests
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
    public void Selects_paid_orders_above_a_threshold()
    {
        var found = OrderQueries.HighValuePaid(Orders, 100m).ToArray();

        found.Select(o => o.Id).ShouldBe(["ord_1", "ord_3", "ord_5"]);
    }

    [Fact]
    public void Returns_nothing_when_nothing_qualifies()
    {
        OrderQueries.HighValuePaid(Orders, 10_000m).ShouldBeEmpty();
    }

    [Fact]
    public void The_query_is_deferred_until_it_is_enumerated()
    {
        // Building a query must not touch the source. This is what makes it
        // composable -- and what makes multiple enumeration so easy to miss.
        var source = new CountingSource<Order>(Orders);

        var query = OrderQueries.HighValuePaid(source, 100m);

        source.Enumerations.ShouldBe(0);

        _ = query.ToArray();

        source.Enumerations.ShouldBe(1);
    }

    [Fact]
    public void Summarising_walks_the_source_exactly_once()
    {
        // The bug this prevents: a method that takes IEnumerable and calls
        // .Any(), .Count() and .Sum() on it enumerates three times. Against a
        // list that is merely wasteful; against a database query or a stream
        // it is three round trips, or three different answers.
        var source = new CountingSource<Order>(Orders);

        OrderQueries.Summarise(source);

        source.Enumerations.ShouldBe(1);
    }

    [Fact]
    public void The_summary_reports_what_it_saw()
    {
        var summary = OrderQueries.Summarise(Orders);

        summary.Count.ShouldBe(5);
        summary.Total.ShouldBe(695m);
        summary.Largest.ShouldBe(300m);
    }

    [Fact]
    public void An_empty_source_summarises_without_throwing()
    {
        // Max() on an empty sequence throws. Aggregating by hand or seeding the
        // aggregate is the difference between a report and an exception.
        var summary = OrderQueries.Summarise([]);

        summary.Count.ShouldBe(0);
        summary.Total.ShouldBe(0m);
        summary.Largest.ShouldBe(0m);
    }
}
