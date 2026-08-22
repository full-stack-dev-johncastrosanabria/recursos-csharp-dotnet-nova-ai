namespace Training.Module03.Core;

/// <summary>
/// Runs a unit of work under a caller's cancellation token.
///
/// Exercise: hand the token to the work, and refuse to start at all if it is
/// already cancelled. Starting work on a cancelled token is not harmless — it
/// still takes a connection, a thread and a retry budget, and then throws the
/// result away. A real failure must still surface as itself, not as a
/// cancellation.
/// </summary>
public static class CancellationRelay
{
    public static Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
