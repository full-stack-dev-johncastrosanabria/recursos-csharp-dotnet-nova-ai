namespace Training.Module01.Challenge;

/// <summary>
/// The same comparison, twice, at different cost.
///
/// The non-generic IComparable parameters force an int to be boxed before the
/// call can even be made. The generic constraint lets the JIT specialise the
/// method for int, calling IComparable&lt;int&gt;.CompareTo directly on the
/// value with nothing on the heap.
/// </summary>
public static class Comparisons
{
    public static object MaxViaInterface(IComparable left, IComparable right)
        => left.CompareTo(right) >= 0 ? left : right;

    public static T Max<T>(T left, T right)
        where T : IComparable<T>
        => left.CompareTo(right) >= 0 ? left : right;
}
