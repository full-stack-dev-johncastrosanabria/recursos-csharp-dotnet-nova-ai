namespace Training.Module03.Challenge;

/// <summary>
/// Accepts work from request threads and processes it on a single background
/// consumer.
///
/// Challenge: build it on System.Threading.Channels. Disposal must complete the
/// channel and wait for the backlog to drain — a queue that drops what it is
/// holding loses work on every deploy, silently, because every EnqueueAsync
/// returned successfully. A handler failure must be held and surfaced at
/// disposal rather than lost on a task nobody observes.
/// </summary>
public sealed class NotificationDispatcher : IAsyncDisposable
{
    public NotificationDispatcher(Func<string, CancellationToken, Task> handler)
        => throw new NotImplementedException();

    public ValueTask EnqueueAsync(string item) => throw new NotImplementedException();

    public ValueTask DisposeAsync() => throw new NotImplementedException();
}
