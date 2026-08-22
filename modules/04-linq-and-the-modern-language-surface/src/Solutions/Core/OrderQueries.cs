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
/// HighValuePaid is deferred because Where and OrderBy are: nothing runs until
/// somebody enumerates. That is what makes queries composable, and also what
/// makes accidental double enumeration so easy — the cost is invisible at the
/// call site.
///
/// Summarise is a hand-written fold rather than Count/Sum/Max, because those
/// three walk the source three times. Over a list that is waste; over a
/// database query it is three round trips; over a stream the three passes can
/// see three different sequences and produce a summary that never existed.
/// Folding also handles the empty case for free, where Max() would throw.
/// </summary>
public static class OrderQueries
{
    public static IEnumerable<Order> HighValuePaid(IEnumerable<Order> orders, decimal threshold)
        => orders.Where(order => order.State == OrderState.Paid && order.Amount > threshold);

    public static OrderSummary Summarise(IEnumerable<Order> orders)
    {
        var count = 0;
        var total = 0m;
        var largest = 0m;

        foreach (var order in orders)
        {
            count++;
            total += order.Amount;
            largest = Math.Max(largest, order.Amount);
        }

        return new OrderSummary(count, total, largest);
    }
}
