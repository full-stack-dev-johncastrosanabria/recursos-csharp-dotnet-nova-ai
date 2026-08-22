// A LINQ query stores what to do, not what was found. It also stores *where to
// look up* the variables it closed over -- not the values they had when you
// wrote it.

var orders = new List<Order>
{
    new("ord_1", "EU", 120m),
    new("ord_2", "US", 300m),
    new("ord_3", "EU", 40m),
};

Console.WriteLine("1. The query closes over the variable, not its value");
Console.WriteLine();

var region = "EU";
var inRegion = orders.Where(o => o.Region == region);

Console.WriteLine($"   built with region = \"{region}\"        -> {Ids(inRegion)}");

region = "US";
Console.WriteLine($"   same query, region now \"{region}\"     -> {Ids(inRegion)}");

Console.WriteLine();
Console.WriteLine("   Nothing re-ran the query on purpose. Enumerating it read `region`");
Console.WriteLine("   again, because that is when the lambda actually runs.");
Console.WriteLine();

Console.WriteLine("2. Materialising freezes the answer");
Console.WriteLine();

region = "EU";
var frozen = orders.Where(o => o.Region == region).ToArray();
region = "US";

Console.WriteLine($"   ToArray() taken while region = \"EU\"  -> {string.Join(", ", frozen.Select(o => o.Id))}");
Console.WriteLine("   The work already happened, so later changes cannot reach it.");
Console.WriteLine();

Console.WriteLine("3. Side effects run once per enumeration, not once per query");
Console.WriteLine();

var touched = 0;
var counted = orders.Select(o => { touched++; return o; });

Console.WriteLine($"   after building the query      touched = {touched}");
_ = counted.ToArray();
Console.WriteLine($"   after enumerating once        touched = {touched}");
_ = counted.ToArray();
Console.WriteLine($"   after enumerating again       touched = {touched}");

Console.WriteLine();
Console.WriteLine("Three consequences worth carrying.");
Console.WriteLine();
Console.WriteLine("  A query returned from a method is a promise to do work later, in the");
Console.WriteLine("  caller's context. If it closes over something that changes -- or over a");
Console.WriteLine("  DbContext that gets disposed -- the failure surfaces at the caller's");
Console.WriteLine("  enumeration, with a stack trace pointing nowhere near the cause.");
Console.WriteLine();
Console.WriteLine("  Deferred is the default and it is the right one: it is what lets you");
Console.WriteLine("  compose Where, OrderBy and Skip into a single database round trip");
Console.WriteLine("  instead of three passes over materialised lists.");
Console.WriteLine();
Console.WriteLine("  So materialise deliberately, not defensively. ToArray() at the point");
Console.WriteLine("  where you want the answer fixed, and leave it deferred everywhere else.");

static string Ids(IEnumerable<Order> orders) => string.Join(", ", orders.Select(o => o.Id));

internal sealed record Order(string Id, string Region, decimal Amount);
