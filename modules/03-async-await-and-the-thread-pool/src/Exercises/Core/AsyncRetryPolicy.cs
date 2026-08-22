namespace Training.Module03.Core;

/// <summary>
/// Retries a failing call a bounded number of times.
///
/// Exercise: attempt numbers start at 1 and are passed to the caller. Stop at
/// maxAttempts and let the last failure escape unchanged. Honour the token
/// between attempts — a retry loop that ignores it keeps hammering a dependency
/// that is already known to be failing, long after the caller has given up.
/// </summary>
public static class AsyncRetryPolicy
{
    public static Task<T> ExecuteAsync<T>(
        Func<int, Task<T>> work,
        int maxAttempts,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
