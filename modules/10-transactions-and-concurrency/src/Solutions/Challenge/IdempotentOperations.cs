namespace Training.Module10.Challenge;

/// <summary>A record of operations already carried out, keyed by the caller's key.</summary>
public interface IOperationLog
{
    /// <summary>The recorded result for this key, or null if it has not run.</summary>
    Task<string?> ResultForAsync(string key);

    Task RecordAsync(string key, string result);
}

/// <summary>
/// Making a retry safe for an operation that is not: record what you did
/// against the caller's key, and let the second attempt be a lookup.
/// </summary>
public static class IdempotentOperations
{
    public static async Task<string> ExecuteOnceAsync(
        IOperationLog log,
        string key,
        Func<Task<string>> work)
    {
        if (await log.ResultForAsync(key) is { } already)
        {
            return already;
        }

        // Deliberately not in a try/catch: a failure must stay retryable, and
        // recording it would make a transient problem permanent.
        var result = await work();
        await log.RecordAsync(key, result);

        return result;
    }
}
