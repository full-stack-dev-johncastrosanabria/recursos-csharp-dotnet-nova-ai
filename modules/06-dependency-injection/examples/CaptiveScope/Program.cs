// The module's real-world case. A singleton takes a scoped dependency in its
// constructor, so the container resolves that dependency once -- from the root
// scope -- and the singleton holds it for the life of the process.
//
// The registration builds without complaint. Nothing fails at startup. The
// symptom arrives later, under concurrency, from a stack trace that names
// whichever request happened to collide.

using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Two schedulers, same dependency, one registration difference.");
Console.WriteLine();

RunBroken();
RunFixed();

Console.WriteLine();
Console.WriteLine("The captive scheduler uses one session for every request forever. For a");
Console.WriteLine("DbContext that means one connection and one change tracker shared by the");
Console.WriteLine("whole process -- which is what produces 'a second operation was started on");
Console.WriteLine("this context' once two requests overlap.");
Console.WriteLine();
Console.WriteLine("Note what makes it expensive rather than merely wrong:");
Console.WriteLine();
Console.WriteLine("  It builds. The default provider validates nothing, so there is no");
Console.WriteLine("  startup error and no warning of any kind.");
Console.WriteLine();
Console.WriteLine("  It works under low traffic. One request at a time never collides, so it");
Console.WriteLine("  passes every test, every review and the first quiet hours of production.");
Console.WriteLine();
Console.WriteLine("  The error names the wrong thing. It surfaces inside whichever request");
Console.WriteLine("  lost the race, so the investigation starts at a handler that is correct.");
Console.WriteLine();
Console.WriteLine("One flag would have refused to build it:");
Console.WriteLine();
ShowValidation();
Console.WriteLine();
Console.WriteLine("The default host turns that on in Development only -- a sensible default,");
Console.WriteLine("and a trap: the check is off in the environment where the bug bites.");

static void RunBroken()
{
    var services = new ServiceCollection();
    services.AddScoped<OrderSession>();
    services.AddSingleton<CaptiveScheduler>();
    using var provider = services.BuildServiceProvider();

    var scheduler = provider.GetRequiredService<CaptiveScheduler>();

    var ids = new HashSet<Guid>();
    for (var request = 0; request < 5; request++)
    {
        ids.Add(scheduler.SessionId);
    }

    var collisions = CountConcurrentFailures(() => scheduler.Session.Query());

    Console.WriteLine("  singleton holding a scoped session (captive)");
    Console.WriteLine($"    distinct sessions across 5 requests   {ids.Count}");
    Console.WriteLine($"    concurrent-use failures observed      {collisions}");
    Console.WriteLine();
}

static void RunFixed()
{
    var services = new ServiceCollection();
    services.AddScoped<OrderSession>();
    services.AddSingleton<ScopedScheduler>();
    using var provider = services.BuildServiceProvider();

    var scheduler = provider.GetRequiredService<ScopedScheduler>();

    var ids = new HashSet<Guid>();
    for (var request = 0; request < 5; request++)
    {
        ids.Add(scheduler.RunOnce());
    }

    var collisions = CountConcurrentFailures(() => scheduler.RunOnce());

    Console.WriteLine("  singleton opening a scope per unit of work (repaired)");
    Console.WriteLine($"    distinct sessions across 5 requests   {ids.Count}");
    Console.WriteLine($"    concurrent-use failures observed      {collisions}");
}

static int CountConcurrentFailures(Action operation)
{
    var failures = 0;

    Parallel.For(0, 24, _ =>
    {
        try
        {
            operation();
        }
        catch (InvalidOperationException)
        {
            Interlocked.Increment(ref failures);
        }
    });

    return failures;
}

static void ShowValidation()
{
    var services = new ServiceCollection();
    services.AddScoped<OrderSession>();
    services.AddSingleton<CaptiveScheduler>();

    try
    {
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });

        Console.WriteLine("    built without complaint");
    }
    catch (AggregateException error)
    {
        // The sentence worth reading is the last one, so print the whole thing.
        foreach (var part in error.InnerExceptions[0].Message.Split(": "))
        {
            Console.WriteLine($"    {part.Trim()}");
        }
    }
}

/// <summary>Stands in for a DbContext: stateful, and not safe to use twice at once.</summary>
internal sealed class OrderSession
{
    private int _inUse;

    public Guid SessionId { get; } = Guid.NewGuid();

    public void Query()
    {
        if (Interlocked.Exchange(ref _inUse, 1) == 1)
        {
            throw new InvalidOperationException(
                "A second operation was started on this context before a previous operation completed.");
        }

        Thread.Sleep(2);
        Interlocked.Exchange(ref _inUse, 0);
    }
}

internal sealed class CaptiveScheduler(OrderSession session)
{
    public OrderSession Session { get; } = session;

    public Guid SessionId => Session.SessionId;
}

internal sealed class ScopedScheduler(IServiceScopeFactory scopeFactory)
{
    public Guid RunOnce()
    {
        using var scope = scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<OrderSession>();
        session.Query();

        return session.SessionId;
    }
}
