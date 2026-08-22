// How an async failure disappears completely.
//
// Nothing here crashes, nothing is logged, and the operation reports success.
// That is the point: a task nobody awaits is a failure nobody hears about.

var observed = new List<string>();
TaskScheduler.UnobservedTaskException += (_, args) =>
{
    lock (observed) { observed.Add(args.Exception.InnerException?.Message ?? "unknown"); }
    args.SetObserved();
};

Console.WriteLine("Three ways to call something that fails.");
Console.WriteLine();

// 1. Fire and forget. The classic.
Console.WriteLine("1. Started and not awaited");
try
{
    _ = ShipAsync("ord_1");
    Console.WriteLine("   caller continued normally, saw no error");
}
catch (InvalidOperationException)
{
    Console.WriteLine("   caught -- this line never runs");
}

// 2. Awaited, which is all it takes.
Console.WriteLine();
Console.WriteLine("2. Awaited");
try
{
    await ShipAsync("ord_2");
}
catch (InvalidOperationException error)
{
    Console.WriteLine($"   caught: {error.Message}");
}

// 3. Held and awaited later -- still fine, the failure waits for you.
Console.WriteLine();
Console.WriteLine("3. Started now, awaited later");
var pending = ShipAsync("ord_3");
Console.WriteLine("   ... other work happens here ...");
try
{
    await pending;
}
catch (InvalidOperationException error)
{
    Console.WriteLine($"   caught: {error.Message}");
}

// The first task is now garbage with an exception nobody ever looked at.
GC.Collect(2, GCCollectionMode.Forced, blocking: true);
GC.WaitForPendingFinalizers();
GC.Collect(2, GCCollectionMode.Forced, blocking: true);
await Task.Delay(100);

Console.WriteLine();
lock (observed)
{
    Console.WriteLine($"Exceptions that reached UnobservedTaskException: {observed.Count}");
    foreach (var message in observed)
    {
        Console.WriteLine($"  - {message}");
    }
}

Console.WriteLine();
Console.WriteLine("Case 1 is the whole lesson. The shipment failed, the caller was told");
Console.WriteLine("nothing, and the only trace is an event handler almost nobody registers --");
Console.WriteLine("which fires whenever the task is eventually collected, minutes later, with");
Console.WriteLine("no request context attached to it.");
Console.WriteLine();
Console.WriteLine("`async void` is the same bug with the safety off. There is no task at all,");
Console.WriteLine("so there is nothing to await and nothing to observe: the exception goes");
Console.WriteLine("straight to the runtime as unhandled and takes the process down. It is not");
Console.WriteLine("demonstrated here because it would end this program. Use it only for event");
Console.WriteLine("handlers, where the signature leaves no choice, and catch everything inside.");
Console.WriteLine();
Console.WriteLine("If you genuinely want to start work and not wait for it, say so in code:");
Console.WriteLine("hold the task, and await it somewhere that can report what happened. That");
Console.WriteLine("is exercise 7 -- a dispatcher that keeps its handler's failure and raises");
Console.WriteLine("it at disposal instead of dropping it.");

static async Task ShipAsync(string orderId)
{
    await Task.Yield();
    throw new InvalidOperationException($"carrier rejected {orderId}");
}
