namespace Training.Module01.Challenge;

/// <summary>
/// Challenge: MaxViaInterface is given and it boxes both arguments. Write Max
/// so it compares the same values without allocating. The difference is one
/// keyword and it is worth understanding rather than memorising.
/// </summary>
public static class Comparisons
{
    public static object MaxViaInterface(IComparable left, IComparable right)
        => left.CompareTo(right) >= 0 ? left : right;

    public static T Max<T>(T left, T right)
        where T : IComparable<T>
        => throw new NotImplementedException();
}
