// A method takes IEnumerable<T> and asks it four questions. Each question is a
// separate walk of the sequence, and nothing at the call site says so.

var orders = new CountingRows<Order>(
[
    new("ord_1", 120m),
    new("ord_2", 40m),
    new("ord_3", 300m),
    new("ord_4", 15m),
]);

Console.WriteLine("Same report, written two ways.");
Console.WriteLine();

orders.Reset();
var naive = Reports.Naive(orders);
Console.WriteLine($"  four LINQ calls    {naive,-34} walks: {orders.Walks}, rows pulled: {orders.RowsRead}");

orders.Reset();
var single = Reports.SinglePass(orders);
Console.WriteLine($"  one fold           {single,-34} walks: {orders.Walks}, rows pulled: {orders.RowsRead}");

Console.WriteLine();
Console.WriteLine("Identical output, four times the work. Against a List<T> that is waste you");
Console.WriteLine("will probably never notice. Against an EF Core query it is four round trips");
Console.WriteLine("to the database. Against a network stream or a reader, the second walk may");
Console.WriteLine("not be possible at all.");
Console.WriteLine();

// The part that is worse than slow.
var ledger = new List<Order> { new("ord_1", 100m) };
var live = ledger.Where(o => o.Amount > 0);

var firstCount = live.Count();
ledger.Add(new Order("ord_2", 50m));
var secondCount = live.Count();

Console.WriteLine("A query is a recipe, not a result. The same query object, enumerated twice");
Console.WriteLine("with an insert in between:");
Console.WriteLine();
Console.WriteLine($"    first count   {firstCount}");
Console.WriteLine($"    second count  {secondCount}");
Console.WriteLine();
Console.WriteLine("So a report that walks its source more than once can produce totals that");
Console.WriteLine("never existed together: a Count() from before the insert and a Sum() from");
Console.WriteLine("after it, printed side by side as though they described one moment.");
Console.WriteLine();
Console.WriteLine("Two habits. Inside a method that receives IEnumerable, walk it once -- fold");
Console.WriteLine("it, or materialise it deliberately with ToArray() and say why. And when a");
Console.WriteLine("caller hands you a query, remember you were handed the recipe, not a meal.");
Console.WriteLine();
Console.WriteLine("Exercise 1 asserts the walk count, because output alone cannot show it.");

internal sealed record Order(string Id, decimal Amount);

internal static class Reports
{
    /// <summary>Reads well. Walks the source four times.</summary>
    public static string Naive(IEnumerable<Order> orders)
        => orders.Any()
            ? $"{orders.Count()} orders, {orders.Sum(o => o.Amount)} total, {orders.Max(o => o.Amount)} largest"
            : "no orders";

    /// <summary>One walk, and the empty case falls out for free.</summary>
    public static string SinglePass(IEnumerable<Order> orders)
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

        return count == 0 ? "no orders" : $"{count} orders, {total} total, {largest} largest";
    }
}

internal sealed class CountingRows<T>(IReadOnlyList<T> rows) : IEnumerable<T>
{
    public int RowsRead { get; private set; }

    public int Walks { get; private set; }

    public void Reset()
    {
        RowsRead = 0;
        Walks = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        Walks++;

        foreach (var row in rows)
        {
            RowsRead++;
            yield return row;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
