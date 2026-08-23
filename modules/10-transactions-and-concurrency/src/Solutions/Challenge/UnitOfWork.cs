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
/// Commit on success, roll back on failure, dispose either way -- and never let
/// a failing rollback hide why you were rolling back.
/// </summary>
public static class UnitOfWork
{
    public static async Task<T> ExecuteAsync<T>(
        ITransactionSource source,
        IsolationLevel level,
        Func<Task<T>> work)
    {
        var transaction = await source.BeginAsync(level);

        await using (transaction.ConfigureAwait(false))
        {
            T result;

            try
            {
                result = await work();
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackFailure)
                {
                    // Swallowed on purpose. The rollback failed because
                    // something is already wrong, and the original exception is
                    // the one that explains the incident.
                    _ = rollbackFailure;
                }

                throw;
            }

            await transaction.CommitAsync();

            return result;
        }
    }
}
