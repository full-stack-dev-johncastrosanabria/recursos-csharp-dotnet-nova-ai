namespace Training.Module03.Core;

/// <summary>
/// Runs work over many items with a ceiling on how many run at once.
///
/// Exercise: process every item, never exceed maxConcurrency, and return the
/// results in input order. Task.WhenAll over a thousand items starts a thousand
/// calls at once; the connection pool that was protecting the database becomes
/// the thing that exhausts it, and the failure appears as timeouts somewhere
/// unrelated.
/// </summary>
public static class BoundedFanOut
{
    public static Task<TOut[]> RunAsync<TIn, TOut>(
        IReadOnlyList<TIn> items,
        int maxConcurrency,
        Func<TIn, Task<TOut>> work,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
