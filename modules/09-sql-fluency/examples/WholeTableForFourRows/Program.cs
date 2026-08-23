// The module's real-world case, read straight out of the plans PostgreSQL
// produced. Nothing here is invented: every number below is parsed from
// EXPLAIN (ANALYZE, FORMAT JSON) output captured against a 200,000-row table,
// and the files are in modules/09-sql-fluency/plans.
//
// Three ways of asking the same question. One of them reads the whole table.

using System.Text.Json;

Console.WriteLine("Find the orders belonging to one email address. Four rows match.");
Console.WriteLine();
Console.WriteLine($"  {"query",-32}{"how",-34}{"rows read",11}{"ms",9}");
Console.WriteLine("  " + new string('-', 88));

Show("lower(email) = '...'", "seq-scan-function-wrapped");
Show("email = '...'", "index-scan-direct");
Show("lower(email), with expr index", "index-scan-expression-index");

Console.WriteLine();
Console.WriteLine("Same answer, same four rows, and the first one looked at fifty thousand");
Console.WriteLine("times as many. A B-tree index stores the COLUMN'S values in order, so it");
Console.WriteLine("can only answer questions about the column. lower(email) is a value the");
Console.WriteLine("index does not contain, so PostgreSQL has to compute it for every row --");
Console.WriteLine("the index is not ignored, it is inapplicable.");
Console.WriteLine();
Console.WriteLine("Two repairs, and they are not equivalent. Row two changes the QUERY to ask");
Console.WriteLine("about the column. Row three leaves the query alone and builds an index on");
Console.WriteLine("the expression, which works and costs something on every insert forever.");
Console.WriteLine();
Console.WriteLine("Note the millisecond column, and how little it tells you. On a warm cache");
Console.WriteLine("with 200,000 rows the difference is small enough to lose in noise, which");
Console.WriteLine("is exactly why this ships. Rows read is the number that scales; at ten");
Console.WriteLine("million rows the first row is still reading all of them.");

static void Show(string label, string fixture)
{
    var path = Path.Combine(AppContext.BaseDirectory, "plans", fixture + ".json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var root = document.RootElement[0];
    var plan = root.GetProperty("Plan");

    var scan = Scans(plan).First();
    var loops = scan.GetProperty("Actual Loops").GetDouble();
    var returned = scan.GetProperty("Actual Rows").GetDouble();
    var removed = scan.TryGetProperty("Rows Removed by Filter", out var r) ? r.GetDouble() : 0;
    var examined = (returned + removed) * loops;

    var how = scan.GetProperty("Node Type").GetString() == "Seq Scan"
        ? "sequential scan"
        : "index: " + IndexName(scan);

    Console.WriteLine(
        $"  {label,-32}{how,-34}{examined,11:N0}{root.GetProperty("Execution Time").GetDouble(),9:F2}");
}

static string IndexName(JsonElement scan)
{
    if (scan.TryGetProperty("Index Name", out var direct))
    {
        return direct.GetString()!;
    }

    foreach (var child in Children(scan))
    {
        if (child.TryGetProperty("Index Name", out var name))
        {
            return name.GetString()!;
        }
    }

    return "?";
}

static IEnumerable<JsonElement> Scans(JsonElement node)
{
    foreach (var candidate in Walk(node))
    {
        var type = candidate.GetProperty("Node Type").GetString()!;
        if (type is "Seq Scan" or "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan")
        {
            yield return candidate;
        }
    }
}

static IEnumerable<JsonElement> Walk(JsonElement node)
{
    yield return node;

    foreach (var child in Children(node))
    {
        foreach (var descendant in Walk(child))
        {
            yield return descendant;
        }
    }
}

static IEnumerable<JsonElement> Children(JsonElement node)
    => node.TryGetProperty("Plans", out var plans)
        ? plans.EnumerateArray()
        : [];
