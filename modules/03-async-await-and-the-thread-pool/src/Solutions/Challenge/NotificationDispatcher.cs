using System.Threading.Channels;

namespace Training.Module03.Challenge;

/// <summary>
/// Accepts work from request threads and processes it on a single background
/// consumer.
///
/// The consumer task is started in the constructor and never awaited until
/// disposal, which is exactly the shape that loses exceptions: nobody observes
/// the task, so a handler failure vanishes. Capturing it in a field and
/// re-throwing at disposal is what makes it visible.
///
/// Completing the writer and then awaiting the consumer is what drains the
/// backlog. Skip it and every deploy silently discards whatever was queued --
/// silently, because each EnqueueAsync had already returned successfully.
/// </summary>
public sealed class NotificationDispatcher : IAsyncDisposable
{
    private readonly Channel<string> _channel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

    private readonly Task _consumer;
    private Exception? _failure;
    private bool _disposed;

    public NotificationDispatcher(Func<string, CancellationToken, Task> handler)
        => _consumer = ConsumeAsync(handler);

    public ValueTask EnqueueAsync(string item)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _channel.Writer.WriteAsync(item);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _channel.Writer.Complete();
        await _consumer;

        if (_failure is not null)
        {
            throw _failure;
        }
    }

    private async Task ConsumeAsync(Func<string, CancellationToken, Task> handler)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync())
        {
            try
            {
                await handler(item, CancellationToken.None);
            }
            catch (Exception error)
            {
                _failure ??= error;
            }
        }
    }
}
