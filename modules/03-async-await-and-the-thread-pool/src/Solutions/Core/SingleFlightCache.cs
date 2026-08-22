namespace Training.Module03.Core;

/// <summary>
/// Caches values by key, collapsing concurrent misses into a single call.
///
/// The idea that makes it work: cache the *task*, not the value. A latecomer
/// finds a call already in flight and awaits that, so twenty simultaneous
/// misses produce one query rather than twenty.
///
/// The idea that makes it correct: remove the entry when the call fails. A
/// cached faulted task is handed to every future caller forever, so one
/// upstream blip becomes a permanently broken key -- and it outlives the
/// upstream recovering, which makes it very hard to explain.
///
/// The ordering below is not cosmetic, and the obvious arrangement is wrong.
/// An async method runs synchronously until its first *incomplete* await, so if
/// the factory hands back an already-faulted task, the cleanup path runs to
/// completion before the calling method gets its task back. Write it as
/// `_entries[key] = Start(...)` and the cleanup removes the entry a moment
/// before that line puts it in -- leaving a failed call cached forever, which
/// is precisely the bug the removal was there to prevent. Publishing the entry
/// first means there is always something for the failure path to remove.
/// </summary>
public sealed class SingleFlightCache<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Task<TValue>> _entries = [];
    private readonly Lock _gate = new();

    public Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> factory)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var completion = new TaskCompletionSource<TValue>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _entries[key] = completion.Task;
            _ = FulfilAsync(key, factory, completion);

            return completion.Task;
        }
    }

    private async Task FulfilAsync(
        TKey key,
        Func<TKey, Task<TValue>> factory,
        TaskCompletionSource<TValue> completion)
    {
        try
        {
            completion.SetResult(await factory(key));
        }
        catch (Exception error)
        {
            lock (_gate)
            {
                _entries.Remove(key);
            }

            completion.SetException(error);
        }
    }
}
