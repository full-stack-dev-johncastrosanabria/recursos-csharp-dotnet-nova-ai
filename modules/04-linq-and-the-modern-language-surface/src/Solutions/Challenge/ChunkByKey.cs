namespace Training.Module04.Challenge;

/// <summary>
/// Groups consecutive items that share a key.
///
/// The public method is deliberately not the iterator. An iterator runs none of
/// its body until the first MoveNext, so argument validation written inside one
/// fires whenever somebody eventually enumerates rather than when they called
/// you — possibly in a different method entirely.
///
/// The iterator holds one chunk at a time, which is the entire point. GroupBy
/// cannot do this: it must see every item before it can return any group,
/// because a matching key may still arrive. Consecutive chunking knows a run is
/// over the moment the key changes, so it can stream.
/// </summary>
public static class ChunkByKey
{
    public static IEnumerable<IReadOnlyList<T>> Chunk<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return Iterate(source, keySelector);

        static IEnumerable<IReadOnlyList<T>> Iterate(
            IEnumerable<T> source,
            Func<T, TKey> keySelector)
        {
            var comparer = EqualityComparer<TKey>.Default;
            List<T>? run = null;
            TKey? runKey = default;

            foreach (var item in source)
            {
                var key = keySelector(item);

                if (run is null)
                {
                    run = [item];
                    runKey = key;
                    continue;
                }

                if (comparer.Equals(runKey!, key))
                {
                    run.Add(item);
                    continue;
                }

                yield return run;
                run = [item];
                runKey = key;
            }

            if (run is not null)
            {
                yield return run;
            }
        }
    }
}
