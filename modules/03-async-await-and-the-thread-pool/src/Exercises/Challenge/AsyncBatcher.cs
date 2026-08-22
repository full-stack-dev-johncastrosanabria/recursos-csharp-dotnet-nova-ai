namespace Training.Module03.Challenge;

/// <summary>
/// Groups an async stream into fixed-size batches.
///
/// Challenge: stay lazy. The reason to batch a stream is that the stream does
/// not fit in memory, so an implementation that materialises the source in
/// order to slice it has thrown away the only property that mattered. Deliver
/// the final partial batch — dropping it is the bug people ship here, and it
/// only appears when the item count stops being a multiple of the batch size.
/// </summary>
public static class AsyncBatcher
{
    public static IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(
        IAsyncEnumerable<T> source,
        int batchSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
