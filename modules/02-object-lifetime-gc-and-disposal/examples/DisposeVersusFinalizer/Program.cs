// Why `using` exists, demonstrated rather than asserted.
//
// A finalizer eventually releases what an object holds. "Eventually" is the
// entire problem: it means at a time the runtime chooses, on a thread you do
// not control, possibly after the process has already run out of the thing you
// were trying to release.

using System.Runtime.CompilerServices;

Console.WriteLine("Releasing a resource two ways.");
Console.WriteLine();

Console.WriteLine("1. Dispose, via a using block");
using (var handle = new DeterministicHandle("db-connection"))
{
    Console.WriteLine($"   inside the block   open: {handle.IsOpen}");
}

Console.WriteLine("   after the block    open: False   <- guaranteed, at that exact line");
Console.WriteLine();

Console.WriteLine("2. A finalizer, on an object that is simply dropped");
CreateAndDrop();
Console.WriteLine($"   immediately after dropping        finalized: {FinalizedHandle.Finalized}");

GC.Collect(2, GCCollectionMode.Forced, blocking: true);
Console.WriteLine($"   after a forced gen-2 collection   finalized: {FinalizedHandle.Finalized}");

GC.WaitForPendingFinalizers();
Console.WriteLine($"   after waiting for finalizers      finalized: {FinalizedHandle.Finalized}");

Console.WriteLine();
Console.WriteLine("Read the middle row again: a full, forced, blocking gen-2 collection ran and");
Console.WriteLine("the object was still not finalized. That is not a timing fluke. A finalizable");
Console.WriteLine("object is not freed when it is found unreachable -- it is put on a queue for");
Console.WriteLine("a separate finalizer thread, survives that collection, and is only released");
Console.WriteLine("by a later one. Adding a finalizer costs every instance an extra collection.");
Console.WriteLine();
Console.WriteLine("Note also what this block needed to show anything at all: a forced collection");
Console.WriteLine("and an explicit wait. In a real process neither happens on demand.");
Console.WriteLine();
Console.WriteLine("So a finalizer is not a cheaper Dispose. It is a last resort for unmanaged");
Console.WriteLine("resources, it makes every instance more expensive to collect, and it cannot");
Console.WriteLine("tell you when it will run. If your type only holds managed resources -- a");
Console.WriteLine("stream, a subscription, a pooled buffer -- it wants IDisposable and nothing");
Console.WriteLine("else. That is every exercise in this module.");
Console.WriteLine();
Console.WriteLine("When a type genuinely needs both, Dispose calls GC.SuppressFinalize(this) so");
Console.WriteLine("that an object released properly does not also pay the finalizer's cost.");

[MethodImpl(MethodImplOptions.NoInlining)]
static void CreateAndDrop()
{
    _ = new FinalizedHandle();
}

internal sealed class DeterministicHandle(string name) : IDisposable
{
    public bool IsOpen { get; private set; } = true;

    public string Name { get; } = name;

    public void Dispose() => IsOpen = false;
}

internal sealed class FinalizedHandle
{
    private static int _finalized;

    public static bool Finalized => Volatile.Read(ref _finalized) > 0;

    ~FinalizedHandle() => Interlocked.Increment(ref _finalized);
}
