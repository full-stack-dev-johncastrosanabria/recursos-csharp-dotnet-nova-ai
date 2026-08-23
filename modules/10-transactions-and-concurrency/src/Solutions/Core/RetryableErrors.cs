namespace Training.Module10.Core;

/// <summary>What a database error means for the caller.</summary>
public enum FailureKind
{
    /// <summary>The database asked you to try again. Nothing is wrong with the request.</summary>
    Retryable,

    /// <summary>A real conflict with real data. Retrying will fail the same way.</summary>
    Conflict,

    /// <summary>The statement is wrong, or the schema is. Retrying is pointless.</summary>
    Fatal,
}

/// <summary>
/// Telling the three kinds of database failure apart. The SQLSTATE class -- the
/// first two characters -- carries most of the meaning.
/// </summary>
public static class RetryableErrors
{
    public static FailureKind Classify(string sqlState)
    {
        if (sqlState.Length < 2)
        {
            return FailureKind.Fatal;
        }

        return sqlState[..2] switch
        {
            // Class 40: transaction rollback. The server is asking for a retry.
            "40" => FailureKind.Retryable,

            // Class 23: integrity constraint. The data really is like that.
            "23" => FailureKind.Conflict,

            // Anything unrecognised is fatal on purpose: repeating an error you
            // do not understand is how one problem becomes a thousand.
            _ => FailureKind.Fatal,
        };
    }

    public static bool ShouldRetry(string sqlState) => Classify(sqlState) == FailureKind.Retryable;
}
