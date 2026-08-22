// The module's real-world case: `.Result` on a task in a request path.
//
// The blocking call itself is not slow. It occupies a thread pool thread and
// then waits for a continuation that needs a thread pool thread to run. Enough
// of those at once and there are no threads left to make progress with, so the
// pool has to inject new ones. It does that deliberately slowly, because a pool
// that grew instantly on demand would thrash. Everything waits meanwhile,
// including work that has nothing to do with the blocking call.

using System.Diagnostics;
using System.Globalization;

const int Requests = 60;

// Starvation happens on a busy production pool, not on an idle laptop with a
// dozen ready threads. Lowering the floor reproduces in seconds what real load
// reproduces at 3am. The ceiling is untouched, so the pool still recovers by
// injecting threads -- which is the actual symptom: not a hang, just slow.
ThreadPool.GetMinThreads(out var originalWorkers, out var originalPorts);
ThreadPool.SetMinThreads(2, originalPorts);

Console.WriteLine($"{Requests} concurrent requests, thread pool floor set to 2 workers.");
Console.WriteLine($"Processor count: {Environment.ProcessorCount}");
Console.WriteLine();
Console.WriteLine($"  {"approach",-24}{"elapsed",10}{"pool threads",15}");
Console.WriteLine("  " + new string('-', 49));

var blocking = Measure("blocking on .Result", () =>
{
    var work = new Task[Requests];
    for (var i = 0; i < Requests; i++)
    {
        work[i] = Task.Run(() =>
        {
            // The line that does the damage. It looks synchronous and harmless.
            var total = ChargeAsync().Result;
            GC.KeepAlive(total);
        });
    }

    Task.WaitAll(work);
});

var awaiting = Measure("awaiting properly", () =>
{
    var work = new Task[Requests];
    for (var i = 0; i < Requests; i++)
    {
        work[i] = Task.Run(async () => GC.KeepAlive(await ChargeAsync()));
    }

    Task.WaitAll(work);
});

ThreadPool.SetMinThreads(originalWorkers, originalPorts);

Console.WriteLine();
Console.WriteLine($"Blocking took {Ratio(blocking, awaiting)}x as long to do identical work.");
Console.WriteLine();
Console.WriteLine("Read the right-hand column first. Both runs did identical work, and one");
Console.WriteLine("of them needed a pile of extra operating-system threads to do it. Those");
Console.WriteLine("threads are not doing anything: each is parked inside .Result, waiting for");
Console.WriteLine("a continuation that is queued behind it. The pool grows because progress");
Console.WriteLine("has stopped, and growing is the only move it has left.");
Console.WriteLine();
Console.WriteLine("Three things make this expensive to diagnose in production.");
Console.WriteLine();
Console.WriteLine("  1. The symptom is not where the cause is. Requests that never touch the");
Console.WriteLine("     blocking code path time out too, because they are queued behind it.");
Console.WriteLine("     The traces blame whichever endpoint happened to be next in line.");
Console.WriteLine();
Console.WriteLine("  2. It is load-dependent. Below the threshold there is always a spare");
Console.WriteLine("     thread and the blocking call is invisible. It passes every test, every");
Console.WriteLine("     staging run, and the first hour of production.");
Console.WriteLine();
Console.WriteLine("  3. CPU is near idle throughout. Every dashboard looks healthy, which");
Console.WriteLine("     rules out the first thing anyone checks.");
Console.WriteLine();
Console.WriteLine("The fix is not a bigger pool. It is not calling .Result -- await it, and");
Console.WriteLine("keep awaiting all the way up. That is what 'async all the way' is for.");

static async Task<decimal> ChargeAsync()
{
    await Task.Delay(50);
    return 19.99m;
}

static TimeSpan Measure(string label, Action run)
{
    var before = ThreadPool.ThreadCount;
    var clock = Stopwatch.StartNew();
    run();
    clock.Stop();

    var grew = ThreadPool.ThreadCount - before;
    var seconds = clock.Elapsed.TotalSeconds.ToString("N2", CultureInfo.InvariantCulture) + " s";
    Console.WriteLine($"  {label,-24}{seconds,10}{$"+{Math.Max(grew, 0)}",15}");
    return clock.Elapsed;
}

static string Ratio(TimeSpan slow, TimeSpan fast)
    => (slow.TotalMilliseconds / Math.Max(fast.TotalMilliseconds, 1))
        .ToString("N1", CultureInfo.InvariantCulture);
