// EXPLAIN reports per loop. Almost nobody reads it that way.
//
// A node showing "rows=4 loops=200" produced 800 rows, and its "actual time"
// is what ONE of those two hundred passes cost. Read those as totals and the
// most expensive thing in the plan looks like the cheapest.

using System.Text.Json;

var path = Path.Combine(AppContext.BaseDirectory, "plans", "nested-loop-per-row-subquery.json");
using var document = JsonDocument.Parse(File.ReadAllText(path));
var root = document.RootElement[0];

Console.WriteLine("A correlated subquery: one lookup per outer row, over 200 rows.");
Console.WriteLine();
Console.WriteLine($"  {"node",-24}{"loops",7}{"rows/loop",11}{"ms/loop",10}{"TOTAL rows",12}{"TOTAL ms",10}");
Console.WriteLine("  " + new string('-', 76));

foreach (var node in Walk(root.GetProperty("Plan")))
{
    var loops = node.GetProperty("Actual Loops").GetDouble();
    var rows = node.GetProperty("Actual Rows").GetDouble();
    var ms = node.GetProperty("Actual Total Time").GetDouble();

    Console.WriteLine(
        $"  {node.GetProperty("Node Type").GetString(),-24}{loops,7:N0}{rows,11:N0}{ms,10:F3}{rows * loops,12:N0}{ms * loops,10:F2}");
}

Console.WriteLine();
Console.WriteLine($"Whole statement: {root.GetProperty("Execution Time").GetDouble():F2} ms");
Console.WriteLine();
Console.WriteLine("Look at the Index Only Scan. It reads 'rows=4' and a hundredth of a");
Console.WriteLine("millisecond -- a perfectly indexed, perfectly fast lookup, and there is");
Console.WriteLine("genuinely nothing wrong with it. It just happened two hundred times.");
Console.WriteLine();
Console.WriteLine("This is the N+1 query, and a plan is where you can actually see it: a");
Console.WriteLine("small fast node with a large loop count. Note what the fix is NOT. Making");
Console.WriteLine("that lookup faster changes nothing worth having, because it is already");
Console.WriteLine("fast; the cost is the repetition. The fix is to stop doing it per row --");
Console.WriteLine("a join, or one query with an IN list.");
Console.WriteLine();
Console.WriteLine("Two hundred rows is a demo. The same query over a page of ten thousand");
Console.WriteLine("customers is ten thousand index lookups, and it degrades linearly with");
Console.WriteLine("exactly the thing you are most likely to grow.");

static IEnumerable<JsonElement> Walk(JsonElement node)
{
    yield return node;

    if (node.TryGetProperty("Plans", out var plans))
    {
        foreach (var child in plans.EnumerateArray())
        {
            foreach (var descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }
}
