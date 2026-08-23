namespace Training.Module10.Challenge;

/// <summary>A record of operations already carried out, keyed by the caller's key.</summary>
public interface IOperationLog
{
    /// <summary>The recorded result for this key, or null if it has not run.</summary>
    Task<string?> ResultForAsync(string key);

    Task RecordAsync(string key, string result);
}

/// <summary>
/// Challenge: make a retry safe for an operation that is not.
///
/// Module 08 stopped short of retrying a POST, because a POST that timed out
/// may already have been applied and repeating it is how one order becomes two.
/// This is the other half of that answer. The rule is not "never retry a
/// write" -- it is "never retry a write the far side cannot recognise".
///
/// An idempotency key is the caller's promise that two requests carrying the
/// same key are the same request. The receiver records what it did against that
/// key, and a repeat returns the recorded answer instead of doing the work
/// again. The retry becomes safe because the SECOND attempt is a lookup.
///
/// ExecuteOnceAsync returns the recorded result if the key has one, without
/// calling work at all. Otherwise it runs work, records the result under the
/// key, and returns it.
///
/// Work that throws must NOT be recorded: a failed attempt has to remain
/// retryable, and recording it would turn a transient failure into a permanent
/// one that every retry faithfully reproduces.
/// </summary>
public static class IdempotentOperations
{
    public static Task<string> ExecuteOnceAsync(IOperationLog log, string key, Func<Task<string>> work)
        => throw new NotImplementedException();
}
