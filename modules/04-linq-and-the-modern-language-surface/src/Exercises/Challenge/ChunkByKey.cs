namespace Training.Module04.Challenge;

/// <summary>
/// Groups consecutive items that share a key.
///
/// Challenge: write a real LINQ operator. GroupBy is not this — it collects
/// every item with the same key wherever it appears, which means buffering the
/// entire source before it can return a single group. Chunking consecutive runs
/// can stream, and streaming is the whole reason to want it.
///
/// Stay deferred: calling this must pull nothing. Stay lazy: taking the first
/// chunk must pull only far enough to know the run has ended.
/// </summary>
public static class ChunkByKey
{
    public static IEnumerable<IReadOnlyList<T>> Chunk<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector)
        => throw new NotImplementedException();
}
