// Two loops that produce identical results, and the one-word difference that
// decides whether the latencies add up or overlap.

using System.Diagnostics;
using System.Globalization;

string[] orderIds = ["ord_1", "ord_2", "ord_3", "ord_4", "ord_5", "ord_6", "ord_7", "ord_8"];

Console.WriteLine($"Enriching {orderIds.Length} orders. Each call takes about 100 ms.");
Console.WriteLine();

var sequential = await Measure("await inside the loop", async () =>
{
    var results = new List<string>();

    foreach (var id in orderIds)
    {
        results.Add(await EnrichAsync(id));
    }

    return results;
});

var concurrent = await Measure("WhenAll after the loop", async () =>
{
    var pending = orderIds.Select(EnrichAsync).ToArray();
    return (await Task.WhenAll(pending)).ToList();
});

Console.WriteLine();
Console.WriteLine("Same eight results, same order, same code shape. The first version waits");
Console.WriteLine("for each call to come back before starting the next, so the total is the");
Console.WriteLine("sum of the latencies. The second starts all eight and then waits once, so");
Console.WriteLine("the total is the slowest of them.");
Console.WriteLine();
Console.WriteLine("The trap is that both are 'async', both compile, and both pass a test that");
Console.WriteLine("only checks the results. Nothing about the slow one looks wrong -- which is");
Console.WriteLine("why exercise 1 asserts on how many calls are in flight at once rather than");
Console.WriteLine("on what comes back.");
Console.WriteLine();
Console.WriteLine("There is a behavioural difference too, not just a speed one. The sequential");
Console.WriteLine("version stops at the first failure, so calls after it never happen. WhenAll");
Console.WriteLine("has already started all of them. If those calls have side effects, the two");
Console.WriteLine("versions do genuinely different things when something goes wrong.");
Console.WriteLine();
Console.WriteLine("And concurrent does not mean unbounded: eight is fine, eight thousand will");
Console.WriteLine("exhaust the connection pool. That is exercise 5.");

static async Task<string> EnrichAsync(string orderId)
{
    await Task.Delay(100);
    return orderId.ToUpperInvariant();
}

static async Task<TimeSpan> Measure(string label, Func<Task<List<string>>> run)
{
    var clock = Stopwatch.StartNew();
    var results = await run();
    clock.Stop();

    var ms = clock.Elapsed.TotalMilliseconds.ToString("N0", CultureInfo.InvariantCulture) + " ms";
    Console.WriteLine($"  {label,-26}{ms,10}   {results.Count} results, first = {results[0]}");
    return clock.Elapsed;
}
