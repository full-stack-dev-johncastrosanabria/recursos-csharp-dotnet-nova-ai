using System.Runtime.CompilerServices;

namespace Training.Module03.Challenge;

/// <summary>
/// Groups an async stream into fixed-size batches.
///
/// The public method is deliberately not the iterator. An async iterator runs
/// none of its body until the first MoveNextAsync, so validation written inside
/// one does not fire when the method is called -- it fires whenever somebody
/// eventually enumerates, which may be in a different method entirely. Doing
/// the check eagerly and delegating to a private iterator is the standard shape
/// for exactly that reason.
///
/// The iterator itself stays lazy, which is the point: batching exists because
/// the stream does not fit in memory, so an implementation that buffers
/// everything in order to slice it has solved nothing.
///
/// The check after the loop delivers the final partial batch. Omitting it is
/// the bug people ship here, and it stays hidden until the item count stops
/// being an exact multiple of the batch size.
/// </summary>
public static class AsyncBatcher
{
    public static IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(
        IAsyncEnumerable<T> source,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        return Iterate(source, batchSize, cancellationToken);

        static async IAsyncEnumerable<IReadOnlyList<T>> Iterate(
            IAsyncEnumerable<T> source,
            int batchSize,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var batch = new List<T>(batchSize);

            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                batch.Add(item);

                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
