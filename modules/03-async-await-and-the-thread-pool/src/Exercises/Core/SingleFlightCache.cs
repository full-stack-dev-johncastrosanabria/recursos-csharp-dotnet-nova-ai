namespace Training.Module03.Core;

/// <summary>
/// Caches values by key, and collapses concurrent misses for the same key into
/// a single underlying call.
///
/// Exercise: twenty simultaneous requests for one uncached key must produce one
/// call, not twenty. The trick is to cache the *task* rather than the value, so
/// that latecomers await the call already in flight. The catch is failure: a
/// cached faulted task turns one upstream blip into a permanently broken key,
/// so a failed call must not be kept.
/// </summary>
public sealed class SingleFlightCache<TKey, TValue>
    where TKey : notnull
{
    public Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> factory)
        => throw new NotImplementedException();
}
