namespace Training.Module01.Challenge;

/// <summary>
/// A list of SKUs iterated without allocating.
///
/// `foreach` binds by pattern, not by interface: the compiler looks for a
/// GetEnumerator whose result has Current and MoveNext, and only falls back to
/// IEnumerable&lt;T&gt; if there is none. A struct enumerator therefore never
/// touches the heap.
/// </summary>
public readonly struct SkuList
{
    private readonly string[] _skus;

    public SkuList(string[] skus) => _skus = skus;

    public int Count => _skus.Length;

    public Enumerator GetEnumerator() => new(_skus);

    public struct Enumerator
    {
        private readonly string[] _skus;
        private int _index;

        internal Enumerator(string[] skus)
        {
            _skus = skus;
            _index = -1;
        }

        public readonly string Current => _skus[_index];

        public bool MoveNext() => ++_index < _skus.Length;
    }
}
