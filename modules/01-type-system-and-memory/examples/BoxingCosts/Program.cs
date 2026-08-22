// The six places boxing hides, each measured rather than asserted. Every number
// below is bytes allocated on the heap by one pass of a loop that, read
// casually, looks like it allocates nothing at all.
//
// Run this before section 4 of the guide, not after.

using System.Collections;
using System.Globalization;

const int Iterations = 1000;

Console.WriteLine($"Six boxing sites, {Iterations} iterations each.");
Console.WriteLine();
Console.WriteLine($"{"site",-46}{"bytes",12}{"per op",10}");
Console.WriteLine(new string('-', 68));

// Worth knowing: writing this as `IMeasurable box = new Reading(i);` in a local
// makes the CA1859 analyser reject the build outright -- it can see the widening
// and tells you to use the concrete type. Passing the struct to a method that
// takes the interface is the same boxing, one call frame away, and no analyser
// complains. That is why this one survives code review in real systems.
Measure("1. interface dispatch on a struct", static () =>
{
    var total = 0;
    for (var i = 0; i < Iterations; i++)
    {
        total += ReadThrough(new Reading(i));
    }

    return total;
});

Measure("2. assignment to object", static () =>
{
    var total = 0;
    for (var i = 0; i < Iterations; i++)
    {
        object boxed = i;
        total += (int)boxed;
    }

    return total;
});

Measure("3. a non-generic collection", static () =>
{
    var list = new ArrayList(Iterations);
    for (var i = 0; i < Iterations; i++)
    {
        list.Add(i);
    }

    return list.Count;
});

Measure("4. params object[]", static () =>
{
    var total = 0;
    for (var i = 0; i < Iterations; i++)
    {
        total += Describe(i, i).Length;
    }

    return total;
});

Measure("5. IComparable without the generic constraint", static () =>
{
    var total = 0;
    for (var i = 0; i < Iterations; i++)
    {
        total += CompareUnconstrained(i, Iterations - i);
    }

    return total;
});

Measure("6. LINQ over value types", static () =>
{
    var numbers = new int[Iterations];
    var total = 0;
    foreach (var value in numbers.Cast<object>())
    {
        total += (int)value;
    }

    return total;
});

Console.WriteLine();
Console.WriteLine("And the same work, written not to box:");
Console.WriteLine();

Measure("   generic constraint instead of IComparable", static () =>
{
    var total = 0;
    for (var i = 0; i < Iterations; i++)
    {
        total += CompareConstrained(i, Iterations - i);
    }

    return total;
});

Measure("   List<int> instead of ArrayList", static () =>
{
    var list = new List<int>(Iterations);
    for (var i = 0; i < Iterations; i++)
    {
        list.Add(i);
    }

    return list.Count;
});

Console.WriteLine();
Console.WriteLine("The loop bodies are the same. The type annotations are not, and that is");
Console.WriteLine("the whole of it. Nothing here is fixed by writing the loop more cleverly.");
Console.WriteLine();
Console.WriteLine("Note the last row is not zero, and should not be: List<int> still allocates");
Console.WriteLine("one backing array. That is one allocation for the whole loop, not one per");
Console.WriteLine("item. ArrayList allocates that array too, and then a box per element on top.");

static void Measure(string label, Func<int> work)
{
    work();
    GC.Collect();
    GC.WaitForPendingFinalizers();

    var before = GC.GetAllocatedBytesForCurrentThread();
    work();
    var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

    var perOperation = ((double)bytes / Iterations).ToString("0.0", CultureInfo.InvariantCulture);
    Console.WriteLine($"{label,-46}{bytes.ToString(CultureInfo.InvariantCulture),12}{perOperation,10}");
}

static int ReadThrough(IMeasurable measurable) => measurable.Value;

static string Describe(params object[] parts) => string.Join(",", parts);

static int CompareUnconstrained(IComparable left, IComparable right) => left.CompareTo(right);

static int CompareConstrained<T>(T left, T right)
    where T : IComparable<T> => left.CompareTo(right);

internal interface IMeasurable
{
    int Value { get; }
}

internal readonly struct Reading(int value) : IMeasurable
{
    public int Value { get; } = value;
}
