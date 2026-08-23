// When a plan makes no sense, the plan is not the problem. The estimate is.
//
// PostgreSQL chooses between a sequential scan and an index, or between a hash
// join and a nested loop, by predicting how many rows each step produces. Those
// predictions come from statistics it keeps on COLUMNS. Ask about an expression
// over a column and there are no statistics to consult, so it falls back to a
// fixed guess -- and every decision downstream is made for a query that does
// not exist.

using System.Text.Json;

Console.WriteLine("Two predicates over the same 200,000-row table.");
Console.WriteLine();
Console.WriteLine($"  {"predicate",-30}{"planner expected",18}{"actually got",14}{"out by",10}");
Console.WriteLine("  " + new string('-', 74));

Show("customer_email = '...'", "index-scan-direct");
Show("total_cents % 4 = 1", "estimate-far-below-actual");

Console.WriteLine();
Console.WriteLine("The second predicate matches a quarter of the table. PostgreSQL guessed");
Console.WriteLine("249 rows and got 50,000 -- and it is the same root cause as the sequential");
Console.WriteLine("scan in the other example. Wrapping a column in an expression takes away");
Console.WriteLine("the index AND the statistics, in one move.");
Console.WriteLine();
Console.WriteLine("Why that matters more than it looks: a bad estimate does not just make");
Console.WriteLine("this node slow. It is an input to every decision above it. A step the");
Console.WriteLine("planner believes returns 249 rows is a fine candidate for a nested loop");
Console.WriteLine("on the other side of a join; at 50,000 rows that same choice is a disaster,");
Console.WriteLine("and the node that looks wrong in the plan is not the one that lied.");
Console.WriteLine();
Console.WriteLine("So when a plan looks stupid, check the estimates before you argue with");
Console.WriteLine("the planner. It made a reasonable choice with the numbers it had. Fix the");
Console.WriteLine("numbers -- ANALYZE, an expression index, extended statistics, or a");
Console.WriteLine("predicate the statistics can actually describe -- and the plan fixes itself.");

static void Show(string label, string fixture)
{
    var path = Path.Combine(AppContext.BaseDirectory, "plans", fixture + ".json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var plan = document.RootElement[0].GetProperty("Plan");

    var worst = Walk(plan)
        .OrderByDescending(n => n.GetProperty("Actual Rows").GetDouble()
            / Math.Max(n.GetProperty("Plan Rows").GetDouble(), 1))
        .First();

    var expected = worst.GetProperty("Plan Rows").GetDouble();
    var actual = worst.GetProperty("Actual Rows").GetDouble();

    Console.WriteLine($"  {label,-30}{expected,18:N0}{actual,14:N0}{actual / Math.Max(expected, 1),9:F0}x");
}

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
