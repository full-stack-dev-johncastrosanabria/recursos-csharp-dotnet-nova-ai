using System.Data;

namespace Training.Module10.Challenge;

/// <summary>A transaction you can finish one of two ways.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();

    Task RollbackAsync();
}

/// <summary>Where transactions come from.</summary>
public interface ITransactionSource
{
    Task<ITransaction> BeginAsync(IsolationLevel level);
}

/// <summary>
/// Challenge: the wrapper that makes "commit on success, roll back on failure"
/// something you cannot forget.
///
/// Every one of these rules exists because somebody shipped the version
/// without it.
///
///   Commit only if the work completed. Obvious, and the reason the wrapper
///   exists at all: a commit at the end of a method runs after an exception is
///   swallowed three lines above it.
///
///   Roll back if it did not, and then RETHROW. Converting a failed
///   transaction into a returned default is how a caller comes to believe
///   something was saved.
///
///   Dispose the transaction whatever happened. An undisposed transaction
///   holds its locks and its connection until something reaps it.
///
///   If the rollback ALSO fails, surface the original exception, not the
///   rollback's. The rollback failed because the connection died; the reason
///   you are rolling back is the thing worth knowing, and losing it to a
///   secondary failure is how an incident becomes unreadable.
/// </summary>
public static class UnitOfWork
{
    public static Task<T> ExecuteAsync<T>(
        ITransactionSource source,
        IsolationLevel level,
        Func<Task<T>> work)
        => throw new NotImplementedException();
}
