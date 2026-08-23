using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>
/// Exercise: what the change tracker is holding, and what it is for.
///
/// Every entity a tracking query returns is put in the context's change
/// tracker, which remembers the values it had when it arrived so SaveChanges
/// can work out what to write. That bookkeeping is the whole point of EF Core,
/// and it is also the source of this module's second failure: the tracker holds
/// the FIRST instance of each key it saw, forever, and hands that same instance
/// back on every later query for the same row.
///
/// AsNoTracking opts out. Note what it does not change: the SQL. Tracking is
/// entirely a client-side decision, so the query on the wire is identical --
/// which is why "the database is fine, the app is slow" is such a common shape
/// on read-heavy screens.
///
/// Tracked and NotTracked return the two forms. TrackedCount reports how many
/// entities the context is currently holding, and StateOf reports one entity's
/// state.
/// </summary>
public static class TrackingBehaviour
{
    public static IQueryable<Order> Tracked(ShopContext db) => throw new NotImplementedException();

    public static IQueryable<Order> NotTracked(ShopContext db) => throw new NotImplementedException();

    public static int TrackedCount(ShopContext db) => throw new NotImplementedException();

    public static EntityState StateOf(ShopContext db, Order order) => throw new NotImplementedException();
}
