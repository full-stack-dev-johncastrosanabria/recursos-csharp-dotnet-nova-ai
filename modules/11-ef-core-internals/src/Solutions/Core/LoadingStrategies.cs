using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>Just enough of an order to show on a list screen.</summary>
public sealed record OrderSummary(int Id, string Reference, int LineCount);

/// <summary>Three ways to get an order and its lines, and the SQL each produces.</summary>
public static class LoadingStrategies
{
    public static IQueryable<Order> WithoutLines(ShopContext db) => db.Orders;

    public static IQueryable<Order> Eager(ShopContext db) => db.Orders.Include(order => order.Lines);

    public static IQueryable<OrderSummary> Projected(ShopContext db)
        => db.Orders.Select(order => new OrderSummary(order.Id, order.Reference, order.Lines.Count));
}
