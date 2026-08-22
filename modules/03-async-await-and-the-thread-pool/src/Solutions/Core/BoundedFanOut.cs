namespace Training.Module03.Core;

/// <summary>
/// Runs work over many items with a ceiling on how many run at once.
///
/// Every task is started immediately, but each one waits on the semaphore
/// before doing anything, so the ceiling is on work in flight rather than on
/// tasks created. Writing results into a pre-sized array by index keeps input
/// order without sorting or extra allocation.
///
/// Task.WhenAll waits for every task to finish before it throws, which is what
/// makes disposing the semaphore below safe: no task is still waiting on it.
/// </summary>
public static class BoundedFanOut
{
    public static async Task<TOut[]> RunAsync<TIn, TOut>(
        IReadOnlyList<TIn> items,
        int maxConcurrency,
        Func<TIn, Task<TOut>> work,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        cancellationToken.ThrowIfCancellationRequested();

        var results = new TOut[items.Count];
        using var limiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var running = new Task[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            running[i] = RunOneAsync(i);
        }

        await Task.WhenAll(running);
        return results;

        async Task RunOneAsync(int index)
        {
            await limiter.WaitAsync(cancellationToken);

            try
            {
                results[index] = await work(items[index]);
            }
            finally
            {
                limiter.Release();
            }
        }
    }
}
