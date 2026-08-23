using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>
/// Including two collections at once returns their product, not their sum.
/// </summary>
public static class SplitQueries
{
    public static IQueryable<Order> BothCollections(ShopContext db)
        => db.Orders
            .Include(order => order.Lines)
            .Include(order => order.Payments);

    public static IQueryable<Order> BothCollectionsSplit(ShopContext db)
        => BothCollections(db).AsSplitQuery();

    // Every line paired with every payment, for every order.
    public static int RowsFromSingleQuery(int orders, int linesEach, int paymentsEach)
        => orders * Math.Max(linesEach, 1) * Math.Max(paymentsEach, 1);

    // One query for the parents, then one per collection.
    public static int RowsFromSplitQuery(int orders, int linesEach, int paymentsEach)
        => orders + (orders * linesEach) + (orders * paymentsEach);
}
