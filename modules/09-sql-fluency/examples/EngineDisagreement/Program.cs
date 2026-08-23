// The same three predicates, run on two engines, read from the plans each one
// produced. Every row below is parsed from a captured plan: PostgreSQL 18 as
// JSON, SQL Server as showplan XML.
//
// The first two rows agree, which is what makes the third expensive.

using System.Text.Json;
using System.Xml.Linq;

Console.WriteLine("One lookup, two engines.");
Console.WriteLine();
Console.WriteLine($"  {"predicate",-30}{"PostgreSQL",-30}{"SQL Server",-30}");
Console.WriteLine("  " + new string('-', 90));

Row("lower(email) = ?", "seq-scan-function-wrapped", "mssql-scan-function-wrapped");
Row("email = ?", "index-scan-direct", "mssql-seek-direct");
Row("cast(placed_at as date) = ?", "seq-scan-cast-to-date", "mssql-seek-cast-to-date");

Console.WriteLine();
Console.WriteLine("Rows one and two are the rule everybody knows: wrap the column and you");
Console.WriteLine("lose the index; ask about the column and you keep it. Both engines agree,");
Console.WriteLine("because the reason is physical -- the index holds the column's values and");
Console.WriteLine("nothing else.");
Console.WriteLine();
Console.WriteLine("Row three is the same predicate and the opposite outcome. SQL Server knows");
Console.WriteLine("that casting a datetime2 to date preserves order, so it rewrites the");
Console.WriteLine("predicate into a range over the raw column and seeks that. The plan admits");
Console.WriteLine("it: a Constant Scan feeding a Nested Loops feeding an Index Seek -- the");
Console.WriteLine("optimiser built the range itself. PostgreSQL has no such rule and reads");
Console.WriteLine("every row.");
Console.WriteLine();
Console.WriteLine("The lesson is not that one engine is cleverer. It is that 'never wrap a");
Console.WriteLine("column in a function' is a rule about indexes, while every exception to it");
Console.WriteLine("is a fact about one optimiser's rewrite rules. The principle travels");
Console.WriteLine("between engines. The exceptions do not, and the exception is exactly where");
Console.WriteLine("somebody arriving from the other database will be confidently wrong.");
Console.WriteLine();
Console.WriteLine("Check, do not assume. Both engines will show you, in seconds.");

static void Row(string label, string postgresFixture, string sqlServerFixture)
    => Console.WriteLine($"  {label,-30}{Postgres(postgresFixture),-30}{SqlServer(sqlServerFixture),-30}");

static string Postgres(string fixture)
{
    var path = Path.Combine(AppContext.BaseDirectory, "plans", fixture + ".json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));

    foreach (var node in WalkJson(document.RootElement[0].GetProperty("Plan")))
    {
        var type = node.GetProperty("Node Type").GetString()!;
        if (type is not ("Seq Scan" or "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan"))
        {
            continue;
        }

        var loops = node.GetProperty("Actual Loops").GetDouble();
        var rows = node.GetProperty("Actual Rows").GetDouble();
        var removed = node.TryGetProperty("Rows Removed by Filter", out var r) ? r.GetDouble() : 0;

        return $"{(type == "Seq Scan" ? "scan" : "index")}, {(rows + removed) * loops:N0} rows";
    }

    return "?";
}

static string SqlServer(string fixture)
{
    XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
    var path = Path.Combine(AppContext.BaseDirectory, "plans", fixture + ".xml");
    var document = XDocument.Load(path);

    var operators = document.Descendants(ns + "RelOp")
        .Select(op => (string)op.Attribute("PhysicalOp")!)
        .ToList();
    var reads = document.Descendants(ns + "RunTimeCountersPerThread")
        .Sum(counter => (long?)counter.Attribute("ActualLogicalReads") ?? 0);

    var how = operators.Any(op => op.Contains("Seek", StringComparison.Ordinal)) ? "seek" : "scan";

    return $"{how}, {reads:N0} logical reads";
}

static IEnumerable<JsonElement> WalkJson(JsonElement node)
{
    yield return node;

    if (node.TryGetProperty("Plans", out var plans))
    {
        foreach (var child in plans.EnumerateArray())
        {
            foreach (var descendant in WalkJson(child))
            {
                yield return descendant;
            }
        }
    }
}
