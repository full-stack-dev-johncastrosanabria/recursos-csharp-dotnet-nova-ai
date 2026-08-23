namespace Training.Module11.Core;

/// <summary>Just enough of an order to show on a list screen.</summary>
public sealed record OrderSummary(int Id, string Reference, int LineCount);

/// <summary>
/// Exercise: three ways to get an order and its lines, and the SQL each one
/// produces.
///
/// Reach for a projection by default. It sends one query, returns only the
/// columns you named, and tracks nothing -- which makes it the right choice for
/// every screen that displays data rather than editing it, and that is most of
/// them. Reach for Include when you genuinely need the entities, because you
/// are going to change them and save. Reach for lazy loading essentially never;
/// it is the module's real-world case.
///
/// WithoutLines queries orders and nothing else. Eager uses Include so the
/// lines come back in the same round trip. Projected selects an OrderSummary
/// per order, counting lines in SQL rather than in memory.
/// </summary>
public static class LoadingStrategies
{
    public static IQueryable<Order> WithoutLines(ShopContext db) => throw new NotImplementedException();

    public static IQueryable<Order> Eager(ShopContext db) => throw new NotImplementedException();

    public static IQueryable<OrderSummary> Projected(ShopContext db) => throw new NotImplementedException();
}
