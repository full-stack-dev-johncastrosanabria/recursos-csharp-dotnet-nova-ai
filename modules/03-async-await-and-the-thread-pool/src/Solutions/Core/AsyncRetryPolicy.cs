namespace Training.Module03.Core;

/// <summary>
/// Retries a failing call a bounded number of times.
///
/// The `when` clause is doing real work. It re-throws on the final attempt
/// without a special case for it, and it deliberately excludes
/// OperationCanceledException -- retrying a cancellation would mean ignoring
/// the caller who just told you to stop.
///
/// Checking the token again after a failure is the difference between a retry
/// loop that stops when the caller gives up and one that keeps hammering a
/// dependency already known to be failing.
/// </summary>
public static class AsyncRetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<int, Task<T>> work,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await work(attempt);
            }
            catch (Exception error)
                when (error is not OperationCanceledException && attempt < maxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
