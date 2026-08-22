namespace Training.Module01.Core;

/// <summary>
/// Two ways to add up the same numbers, one of which allocates.
///
/// `foreach` over IReadOnlyList&lt;T&gt; calls GetEnumerator through the
/// interface, which boxes List&lt;T&gt;'s struct enumerator onto the heap once
/// per call. Taking the concrete List&lt;T&gt; lets the compiler bind to the
/// struct enumerator directly and the allocation disappears.
/// </summary>
public static class OrderTotals
{
    public static decimal SumViaInterface(IReadOnlyList<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }

    public static decimal SumWithoutAllocating(List<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }
}
