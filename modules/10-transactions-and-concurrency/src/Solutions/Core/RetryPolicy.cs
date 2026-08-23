namespace Training.Module10.Core;

/// <summary>A database error, carrying the SQLSTATE that explains it.</summary>
public sealed class DatabaseFailureException(string sqlState) : Exception($"database error {sqlState}")
{
    public string SqlState { get; } = sqlState;
}

/// <summary>How an operation ended, after however many attempts it took.</summary>
public sealed record RetryOutcome(bool Succeeded, int Attempts, string? LastSqlState);

/// <summary>
/// The retry loop that makes higher isolation levels usable. A database raising
/// serialization failures is refusing to give you a wrong answer and expecting
/// you to come back; this is coming back.
/// </summary>
public static class RetryPolicy
{
    public const int MaxAttempts = 4;

    public static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(50);

    public static TimeSpan DelayFor(int attempt, double jitter)
        => BaseDelay * Math.Pow(2, attempt - 1) * (0.5 + jitter);

    public static async Task<RetryOutcome> ExecuteAsync(Func<Task> operation, Func<TimeSpan, Task> delay)
    {
        string? lastSqlState = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await operation();

                return new RetryOutcome(true, attempt, null);
            }
            catch (DatabaseFailureException failure)
            {
                lastSqlState = failure.SqlState;

                // A conflict or a fatal error will fail identically next time.
                if (!RetryableErrors.ShouldRetry(failure.SqlState))
                {
                    return new RetryOutcome(false, attempt, lastSqlState);
                }

                // Between attempts only: nobody benefits from waiting after the
                // last one.
                if (attempt < MaxAttempts)
                {
                    await delay(DelayFor(attempt, Random.Shared.NextDouble()));
                }
            }
        }

        return new RetryOutcome(false, MaxAttempts, lastSqlState);
    }
}
