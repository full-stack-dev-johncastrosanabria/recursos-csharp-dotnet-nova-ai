namespace Training.Module04.Core;

public enum OrderState
{
    Pending,
    Paid,
    Cancelled,
}

public sealed record Order(string Id, string Region, decimal Amount, OrderState State);

public sealed record OrderSummary(int Count, decimal Total, decimal Largest);

/// <summary>
/// Queries over a sequence of orders.
///
/// Exercise: HighValuePaid must stay deferred — building it touches nothing,
/// and enumerating it walks the source once. Summarise must walk the source
/// exactly once too, which rules out the obvious version that calls Count(),
/// Sum() and Max() in turn. Over a list that is merely wasteful; over a
/// database query it is three round trips, and over a stream the three calls
/// can see three different sequences.
///
/// Max() on an empty sequence throws, so an empty source is its own case.
/// </summary>
public static class OrderQueries
{
    public static IEnumerable<Order> HighValuePaid(IEnumerable<Order> orders, decimal threshold)
        => throw new NotImplementedException();

    public static OrderSummary Summarise(IEnumerable<Order> orders)
        => throw new NotImplementedException();
}
