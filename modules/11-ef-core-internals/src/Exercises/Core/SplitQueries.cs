namespace Training.Module11.Core;

/// <summary>
/// Exercise: what happens when you Include two collections at once.
///
/// One query joining a parent to two child collections does not return
/// orders + lines + payments rows. It returns their PRODUCT, because that is
/// what a join of two independent one-to-many relationships is: every line
/// paired with every payment, for every order. EF then discards the duplicates
/// while materialising, so the object graph is correct and only the network,
/// the memory and the wall clock suffer.
///
/// AsSplitQuery sends one query per collection instead. The cost is that the
/// queries are no longer a single consistent read, so a concurrent write
/// between them can produce a graph that never existed in the database. That is
/// the trade you are making, and it is a real one -- not a free improvement.
///
/// BothCollections includes lines and payments in a single query;
/// BothCollectionsSplit is the same query split. RowsFromSingleQuery and
/// RowsFromSplitQuery return the row counts each strategy pulls over the wire
/// for the given shape.
/// </summary>
public static class SplitQueries
{
    public static IQueryable<Order> BothCollections(ShopContext db) => throw new NotImplementedException();

    public static IQueryable<Order> BothCollectionsSplit(ShopContext db) => throw new NotImplementedException();

    public static int RowsFromSingleQuery(int orders, int linesEach, int paymentsEach)
        => throw new NotImplementedException();

    public static int RowsFromSplitQuery(int orders, int linesEach, int paymentsEach)
        => throw new NotImplementedException();
}
