// Transient does not mean short-lived. It means "a new one every time you ask",
// and the container still owns every disposable it created -- until the scope
// that created it ends.
//
// Resolve a transient disposable from the root provider and the scope that
// created it is the container itself, which ends when the process does. The
// instances are not leaked in the sense of being lost: they are held, on
// purpose, by the provider's list of things it must dispose. That list is a GC
// root, so nothing on it can ever be collected.

using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("One singleton, one transient connection per operation, 2000 operations.");
Console.WriteLine("The only difference is where the connection is resolved from.");
Console.WriteLine();
Console.WriteLine($"  {"resolved from",-34}{"created",9}{"disposed",10}{"still held",12}");
Console.WriteLine("  " + new string('-', 65));

RunFromRootProvider();
RunFromAScopePerOperation();

Console.WriteLine();
Console.WriteLine("Both rows created the same 2000 connections. The first row disposed none of");
Console.WriteLine("them, because the root provider is their scope and it is still open -- it");
Console.WriteLine("closes when the process exits. Under real traffic that number does not");
Console.WriteLine("plateau; it tracks total requests served, and the process dies at whatever");
Console.WriteLine("hour that crosses the memory limit.");
Console.WriteLine();
Console.WriteLine("Note what the first row is NOT. It is not a missing Dispose call, and no");
Console.WriteLine("using statement would have helped -- the container, not the caller, owns");
Console.WriteLine("what the container created. It is not a GC problem either:");
Console.WriteLine();
ShowShutdownDisposal();
Console.WriteLine();
Console.WriteLine("Everything is disposed correctly, at exactly the moment the container");
Console.WriteLine("promises. The promise was just far longer than the author assumed.");
Console.WriteLine();
Console.WriteLine("The shape to recognise in review: a singleton that injects IServiceProvider");
Console.WriteLine("and resolves per operation. That provider is the root one. Inject");
Console.WriteLine("IServiceScopeFactory and open a scope per unit of work instead -- the same");
Console.WriteLine("repair the captive-dependency case needs, for a different reason.");

static void RunFromRootProvider()
{
    var tally = new Tally();
    using var provider = BuildProvider(tally);

    // Injecting IServiceProvider into a singleton hands it the ROOT provider.
    var writer = new ReportWriter(provider);
    for (var operation = 0; operation < 2000; operation++)
    {
        writer.WriteFromRoot();
    }

    Report("the root provider", tally);
}

static void RunFromAScopePerOperation()
{
    var tally = new Tally();
    using var provider = BuildProvider(tally);

    var writer = new ReportWriter(provider);
    for (var operation = 0; operation < 2000; operation++)
    {
        writer.WriteFromOwnScope();
    }

    Report("a scope per operation", tally);
}

static void ShowShutdownDisposal()
{
    var tally = new Tally();
    var provider = BuildProvider(tally);

    var writer = new ReportWriter(provider);
    for (var operation = 0; operation < 2000; operation++)
    {
        writer.WriteFromRoot();
    }

    Console.WriteLine($"    before the container is disposed   {tally.Live,5} still held");

    provider.Dispose();

    Console.WriteLine($"    after                              {tally.Live,5} still held");
}

static ServiceProvider BuildProvider(Tally tally)
{
    var services = new ServiceCollection();
    services.AddSingleton(tally);
    services.AddTransient<PooledConnection>();

    return services.BuildServiceProvider();
}

static void Report(string label, Tally tally)
    => Console.WriteLine($"  {label,-34}{tally.Created,9}{tally.Disposed,10}{tally.Live,12}");

/// <summary>Counts construction against disposal, so "still held" is measured rather than asserted.</summary>
internal sealed class Tally
{
    public int Created { get; private set; }

    public int Disposed { get; private set; }

    public int Live => Created - Disposed;

    public void NoteCreated() => Created++;

    public void NoteDisposed() => Disposed++;
}

/// <summary>Stands in for anything transient that holds an unmanaged handle: a connection, a socket, a stream.</summary>
internal sealed class PooledConnection : IDisposable
{
    private readonly Tally _tally;

    public PooledConnection(Tally tally)
    {
        _tally = tally;
        tally.NoteCreated();
    }

    public void Dispose() => _tally.NoteDisposed();
}

/// <summary>
/// A singleton doing a unit of work per operation -- the shape this example is about.
/// </summary>
internal sealed class ReportWriter(IServiceProvider provider)
{
    /// <summary>The bug: resolved from the root provider, so the root provider owns it.</summary>
    public void WriteFromRoot()
    {
        var connection = provider.GetRequiredService<PooledConnection>();
        Use(connection);
    }

    /// <summary>The repair: a scope per unit of work, disposed when the work ends.</summary>
    public void WriteFromOwnScope()
    {
        using var scope = provider.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<PooledConnection>();
        Use(connection);
    }

    private static void Use(PooledConnection connection) => _ = connection;
}
