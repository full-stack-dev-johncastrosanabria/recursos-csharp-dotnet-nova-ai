using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>
/// What the change tracker is holding, and what it is for. Note that
/// AsNoTracking does not change the SQL: tracking is a client-side decision.
/// </summary>
public static class TrackingBehaviour
{
    public static IQueryable<Order> Tracked(ShopContext db) => db.Orders;

    public static IQueryable<Order> NotTracked(ShopContext db) => db.Orders.AsNoTracking();

    public static int TrackedCount(ShopContext db) => db.ChangeTracker.Entries().Count();

    public static EntityState StateOf(ShopContext db, Order order) => db.Entry(order).State;
}
