// The module's real-world case. A static dictionary caching orders by id, with
// nothing that ever removes an entry.
//
// Read the table as a sequence. The row that matters is the third: a full,
// blocking, gen-2 collection runs and frees nothing, because nothing is
// garbage. Every order is still reachable from a static field, and reachable
// is the only question the collector asks.

using System.Globalization;

const int Orders = 200_000;

Console.WriteLine("One static cache, filled, collected, then released.");
Console.WriteLine();
Console.WriteLine($"{"step",-46}{"entries",9}{"live heap",13}");
Console.WriteLine(new string('-', 68));

Report("baseline, nothing cached", UnboundedCache.Count);

for (var i = 0; i < Orders; i++)
{
    UnboundedCache.Put($"ord_{i}", new OrderRecord($"ord_{i}", i, new byte[64]));
}

Report($"after {Orders:N0} orders, no eviction", UnboundedCache.Count);

GC.Collect(2, GCCollectionMode.Forced, blocking: true);
GC.WaitForPendingFinalizers();
Report("after a forced gen-2 collection", UnboundedCache.Count);

UnboundedCache.Clear();
Report("after clearing the static field", UnboundedCache.Count);

var bounded = new RingCache(500);
for (var i = 0; i < Orders; i++)
{
    bounded.Put($"ord_{i}", new OrderRecord($"ord_{i}", i, new byte[64]));
}

Report($"after {Orders:N0} orders, bounded to 500", bounded.Count);
GC.KeepAlive(bounded);

Console.WriteLine();
Console.WriteLine("Rows 2 and 3 are the bug: the collector ran and the number did not move.");
Console.WriteLine("Row 4 is the proof that nothing was ever broken -- the moment the reference");
Console.WriteLine("goes, the memory goes. It was reachable the whole time, exactly as designed.");
Console.WriteLine();
Console.WriteLine("That is what makes this expensive to find. A leak detector looks for");
Console.WriteLine("unreachable memory and reports none. A heap profiler shows a large, healthy,");
Console.WriteLine("fully-referenced dictionary, which is what the code asked for. The process");
Console.WriteLine("just grows until it is killed, and restarting it 'fixes' the problem.");
Console.WriteLine();
Console.WriteLine("The bug is a missing decision, not a missing free. Nobody chose what happens");
Console.WriteLine("when the cache fills, which is the same as choosing that it never does.");
Console.WriteLine("Row 5 is the repair, and it is exercise 2.");

static void Report(string label, int entries)
{
    var live = GC.GetTotalMemory(forceFullCollection: true);
    var megabytes = (live / 1024d / 1024d).ToString("N1", CultureInfo.InvariantCulture) + " MB";

    Console.WriteLine(
        $"{label,-46}{entries.ToString("N0", CultureInfo.InvariantCulture),9}{megabytes,13}");
}

internal sealed record OrderRecord(string OrderId, int Sequence, byte[] Payload);

/// <summary>The shape that ships the bug: static, and nothing ever removes a key.</summary>
internal static class UnboundedCache
{
    private static readonly Dictionary<string, OrderRecord> Entries = new(StringComparer.Ordinal);

    public static int Count => Entries.Count;

    public static void Clear() => Entries.Clear();

    public static void Put(string orderId, OrderRecord order) => Entries[orderId] = order;
}

/// <summary>The same cache with a decision in it.</summary>
internal sealed class RingCache(int capacity)
{
    private readonly Dictionary<string, OrderRecord> _entries = new(StringComparer.Ordinal);
    private readonly Queue<string> _arrivals = new();

    public int Count => _entries.Count;

    public void Put(string orderId, OrderRecord order)
    {
        if (!_entries.TryAdd(orderId, order))
        {
            _entries[orderId] = order;
            return;
        }

        _arrivals.Enqueue(orderId);

        if (_entries.Count > capacity)
        {
            _entries.Remove(_arrivals.Dequeue());
        }
    }
}
