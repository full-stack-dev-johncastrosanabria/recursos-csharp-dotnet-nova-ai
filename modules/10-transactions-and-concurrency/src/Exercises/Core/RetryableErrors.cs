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
/// Exercise: tell the three kinds of database failure apart.
///
/// This is the decision every data-access layer gets wrong at least once, in
/// one of two directions. Retry everything and a duplicate-key error becomes
/// five duplicate-key errors and a slower failure. Retry nothing and a
/// perfectly healthy serialization conflict -- which the database raised
/// precisely BECAUSE it expects you to try again -- surfaces to a customer as
/// an error.
///
/// PostgreSQL reports these as five-character SQLSTATEs, and the class (the
/// first two characters) carries most of the meaning. The ones that matter
/// here, all observed against a live server in this module's integration tier:
///
///   40001  serialization_failure   -- retryable
///   40P01  deadlock_detected       -- retryable
///   23505  unique_violation        -- a conflict; the row really does exist
///   23502  not_null_violation      -- a conflict; the data really is missing
///   23503  foreign_key_violation   -- a conflict
///   42P01  undefined_table         -- fatal, and so is anything else in 42
///
/// Classify anything in class 40 as Retryable, anything in class 23 as
/// Conflict, and everything else as Fatal. Unknown SQLSTATEs are Fatal:
/// guessing that an unrecognised error is safe to repeat is how a retry loop
/// turns one problem into a thousand.
/// </summary>
public static class RetryableErrors
{
    public static FailureKind Classify(string sqlState) => throw new NotImplementedException();

    public static bool ShouldRetry(string sqlState) => throw new NotImplementedException();
}
