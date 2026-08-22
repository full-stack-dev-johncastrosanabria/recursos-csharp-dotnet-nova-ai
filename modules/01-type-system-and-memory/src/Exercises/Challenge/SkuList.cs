namespace Training.Module01.Challenge;

/// <summary>
/// A list of SKUs that can be iterated without allocating.
///
/// Challenge: `foreach` does not require IEnumerable&lt;T&gt;. It binds by
/// pattern to any GetEnumerator returning a type with Current and MoveNext.
/// Make that enumerator a struct and iteration allocates nothing.
/// </summary>
public readonly struct SkuList
{
    private readonly string[] _skus;

    public SkuList(string[] skus) => _skus = skus;

    public int Count => throw new NotImplementedException();

    public Enumerator GetEnumerator() => throw new NotImplementedException();

    public struct Enumerator
    {
        public readonly string Current => throw new NotImplementedException();

        public bool MoveNext() => throw new NotImplementedException();
    }
}
