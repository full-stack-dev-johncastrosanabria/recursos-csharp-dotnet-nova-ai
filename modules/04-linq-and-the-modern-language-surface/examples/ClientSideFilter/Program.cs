// The module's real-world case, and the uncomfortable part is the first half of
// the output: in memory, the two versions are indistinguishable. Same results,
// same rows read, same everything. Your unit tests cannot tell them apart, and
// that is exactly why this reaches production.
//
// The second half shows the only thing that differs before deployment: what
// the database is actually asked for.


var rows = new CountingRows<Order>(
[
    new("ord_1", "EU", 120m, "Paid"),
    new("ord_2", "EU", 40m, "Pending"),
    new("ord_3", "US", 300m, "Paid"),
    new("ord_4", "US", 15m, "Cancelled"),
    new("ord_5", "EU", 220m, "Paid"),
]);

Console.WriteLine("Two implementations of 'paid orders over 100'.");
Console.WriteLine();

rows.Reset();
var serverSide = Queries.ServerSide(rows.AsQueryable(), 100m).ToArray();
var serverSideRows = rows.RowsRead;

rows.Reset();
var clientSide = Queries.ClientSide(rows.AsQueryable(), 100m).ToArray();
var clientSideRows = rows.RowsRead;

Console.WriteLine("  In memory, which is where your tests run:");
Console.WriteLine($"    same results          {serverSide.SequenceEqual(clientSide)}");
Console.WriteLine($"    rows read, server-side version   {serverSideRows}");
Console.WriteLine($"    rows read, client-side version   {clientSideRows}");
Console.WriteLine();
Console.WriteLine("  Identical. There is no assertion about results or row counts that");
Console.WriteLine("  separates them, because in memory there is no boundary to cross.");
Console.WriteLine();

Console.WriteLine("  What a database provider is handed:");
Console.WriteLine();
Console.WriteLine("    server-side version");
Console.WriteLine($"      {Describe(Queries.ServerSide(rows.AsQueryable(), 100m))}");
Console.WriteLine();
Console.WriteLine("    client-side version");
Console.WriteLine($"      {Describe(Queries.ClientSide(rows.AsQueryable(), 100m))}");
Console.WriteLine();

Console.WriteLine("The first tree carries the filter, so it becomes a WHERE clause and the");
Console.WriteLine("database returns three rows. The second stops at the table: everything");
Console.WriteLine("after AsEnumerable runs in your process, so the database is asked for the");
Console.WriteLine("whole table and the filtering happens after all of it has crossed the wire.");
Console.WriteLine();
Console.WriteLine("On five rows that is nothing. On five million it is an outage, and it");
Console.WriteLine("arrives without a code change -- the table simply grew.");
Console.WriteLine();
Console.WriteLine("The same thing happens without AsEnumerable ever appearing. A helper typed");
Console.WriteLine("Func<Order, bool> instead of Expression<Func<Order, bool>> is opaque to the");
Console.WriteLine("provider, so it silently forces the identical fallback -- through a change");
Console.WriteLine("that looks like ordinary refactoring.");
Console.WriteLine();
Console.WriteLine("Exercise 6 asserts on the expression tree for this reason. It is the only");
Console.WriteLine("check that fails before production.");

static string Describe<T>(IQueryable<T> query)
{
    var text = query.Expression.ToString();
    return text.Length <= 150 ? text : text[..150] + " ...";
}

internal sealed record Order(string Id, string Region, decimal Amount, string State);

internal static class Queries
{
    /// <summary>Stays an IQueryable, so the filter reaches the provider.</summary>
    public static IQueryable<Order> ServerSide(IQueryable<Order> orders, decimal threshold)
        => orders.Where(o => o.State == "Paid" && o.Amount > threshold);

    /// <summary>
    /// The bug. AsEnumerable ends the queryable part of the chain; everything
    /// after it is LINQ-to-Objects running locally.
    /// </summary>
    public static IQueryable<Order> ClientSide(IQueryable<Order> orders, decimal threshold)
        => orders.AsEnumerable().Where(o => o.State == "Paid" && o.Amount > threshold).AsQueryable();
}

/// <summary>Counts how many rows are pulled out of the underlying sequence.</summary>
internal sealed class CountingRows<T>(IReadOnlyList<T> rows) : IEnumerable<T>
{
    public int RowsRead { get; private set; }

    public void Reset() => RowsRead = 0;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var row in rows)
        {
            RowsRead++;
            yield return row;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
