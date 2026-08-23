namespace Training.Module10.Core;

/// <summary>A database error, carrying the SQLSTATE that explains it.</summary>
public sealed class DatabaseFailureException(string sqlState) : Exception($"database error {sqlState}")
{
    public string SqlState { get; } = sqlState;
}

/// <summary>How an operation ended, after however many attempts it took.</summary>
public sealed record RetryOutcome(bool Succeeded, int Attempts, string? LastSqlState);

/// <summary>
/// Exercise: the retry loop that makes higher isolation levels usable.
///
/// A database that raises serialization failures is not misbehaving -- it is
/// refusing to give you a wrong answer, and expecting you to come back. That
/// bargain only works if you actually come back, which means every write path
/// running above READ COMMITTED needs one of these. Without it, raising the
/// isolation level converts silent corruption into loud failure, which is
/// better but not good.
///
/// Two rules the exercises hold you to. Retry ONLY what
/// RetryableErrors.ShouldRetry allows -- a duplicate key retried four times is
/// four identical errors and a slower response. And back off exponentially with
/// jitter: without jitter, every client that collided retries at the same
/// instant and collides again, which is how a brief contention spike becomes a
/// sustained one.
///
/// DelayFor returns BaseDelay x 2^(attempt-1) x (0.5 + jitter), where attempt
/// is 1-based and jitter is between 0 and 1 -- so the delay lands anywhere from
/// half to one and a half times the nominal backoff.
///
/// ExecuteAsync runs operation, at most MaxAttempts times. It awaits delay
/// BETWEEN attempts and not after the last one. It returns rather than throws:
/// a successful outcome after three tries and a conflict that will never
/// succeed are both answers the caller needs, not exceptions.
/// </summary>
public static class RetryPolicy
{
    public const int MaxAttempts = 4;

    public static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(50);

    public static TimeSpan DelayFor(int attempt, double jitter) => throw new NotImplementedException();

    public static Task<RetryOutcome> ExecuteAsync(Func<Task> operation, Func<TimeSpan, Task> delay)
        => throw new NotImplementedException();
}
