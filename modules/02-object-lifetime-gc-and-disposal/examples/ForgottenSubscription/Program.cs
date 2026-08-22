// The most common managed leak in .NET, and the reason it hides: the object
// that leaks is not the one you are looking at.
//
// A short-lived subscriber attaches a handler to a long-lived publisher. The
// publisher's invocation list holds the handler; the handler holds the
// subscriber; the subscriber holds whatever it holds. Drop every reference you
// have to the subscriber and it stays alive anyway -- rooted by an object you
// were not thinking about.

using System.Runtime.CompilerServices;

Console.WriteLine("A subscriber is created, then dropped. Is it collected?");
Console.WriteLine();
Console.WriteLine("Each row uses its own feed, so the two scenarios cannot affect each other.");
Console.WriteLine();

// A feed per scenario: sharing one would leave the first row's leaked handler
// attached during the second, which is true but reads like the -= failed.
var leakingFeed = new PriceFeed();
var leaked = AttachAndForget(leakingFeed, unsubscribe: false);
Collect();
Console.WriteLine($"  never unsubscribed      handlers still on feed: {leakingFeed.SubscriberCount}   subscriber alive: {IsAlive(leaked)}");

var tidyFeed = new PriceFeed();
var released = AttachAndForget(tidyFeed, unsubscribe: true);
Collect();
Console.WriteLine($"  unsubscribed on close   handlers still on feed: {tidyFeed.SubscriberCount}   subscriber alive: {IsAlive(released)}");

Console.WriteLine();
Console.WriteLine("The first subscriber is unreachable from your code and still alive. Every");
Console.WriteLine("byte it holds -- its buffer here, a DbContext or an HTTP client in real code");
Console.WriteLine("-- is alive with it, and none of it will ever be collected while the feed");
Console.WriteLine("lives. A long-lived publisher turns every forgotten -= into a permanent leak.");
Console.WriteLine();
Console.WriteLine("Two details worth carrying away.");
Console.WriteLine();
Console.WriteLine("  1. The leaking object is the subscriber, but the reference that roots it");
Console.WriteLine("     belongs to the publisher. Searching the subscriber's own code for the");
Console.WriteLine("     mistake finds nothing, because the mistake is an absence.");
Console.WriteLine();
Console.WriteLine("  2. Store the delegate in a field. Writing `feed.PriceChanged += p => ...`");
Console.WriteLine("     and later `feed.PriceChanged -= p => ...` creates two different");
Console.WriteLine("     delegate instances, so the -= removes nothing at all -- and the code");
Console.WriteLine("     reads as though it unsubscribes. Exercise 5 is this, as a test.");

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference<Watcher> AttachAndForget(PriceFeed feed, bool unsubscribe)
{
    var watcher = new Watcher(feed);
    feed.Publish(19.99m);

    if (unsubscribe)
    {
        watcher.Dispose();
    }

    // The only reference kept is weak, so it does not root the watcher itself.
    return new WeakReference<Watcher>(watcher);
}

static bool IsAlive(WeakReference<Watcher> reference) => reference.TryGetTarget(out _);

static void Collect()
{
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
}

internal sealed class PriceFeed
{
    public event EventHandler<decimal>? PriceChanged;

    public int SubscriberCount => PriceChanged?.GetInvocationList().Length ?? 0;

    public void Publish(decimal price) => PriceChanged?.Invoke(this, price);
}

internal sealed class Watcher : IDisposable
{
    private readonly PriceFeed _feed;
    private readonly EventHandler<decimal> _handler;

    // Stands in for whatever the subscriber holds: a context, a client, a cache.
    private readonly byte[] _buffer = new byte[1024];

    private bool _disposed;

    public Watcher(PriceFeed feed)
    {
        _feed = feed;
        _handler = OnPriceChanged;
        _feed.PriceChanged += _handler;
    }

    public decimal Last { get; private set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _feed.PriceChanged -= _handler;
    }

    private void OnPriceChanged(object? sender, decimal price)
    {
        Last = price;
        _buffer[0] = 1;
    }
}
