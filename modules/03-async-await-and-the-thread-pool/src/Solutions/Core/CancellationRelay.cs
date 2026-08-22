namespace Training.Module03.Core;

/// <summary>
/// Runs a unit of work under a caller's cancellation token.
///
/// Two lines, and both matter. The check before starting means work is never
/// begun on a token that is already cancelled -- that work would still take a
/// connection, a thread and a retry budget before its result was discarded.
/// Passing the token onward is what makes cancellation reach the bottom of the
/// stack; a token that stops at the first method is decoration.
/// </summary>
public static class CancellationRelay
{
    public static async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await work(cancellationToken);
    }
}
