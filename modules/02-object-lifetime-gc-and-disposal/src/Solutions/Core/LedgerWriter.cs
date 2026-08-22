namespace Training.Module02.Core;

/// <summary>
/// Buffers ledger entries and writes them out in batches.
///
/// Anything that buffers owes its callers a flush on the way out. Every
/// WriteAsync here returns successfully, so a writer that drops its buffer on
/// disposal loses data that the caller was told had been accepted.
/// </summary>
public sealed class LedgerWriter : IAsyncDisposable
{
    private readonly IList<string> _sink;
    private readonly int _flushThreshold;
    private readonly List<string> _buffer = [];
    private bool _disposed;

    public LedgerWriter(IList<string> sink, int flushThreshold)
    {
        _sink = sink;
        _flushThreshold = flushThreshold;
    }

    public int Pending => _buffer.Count;

    public ValueTask WriteAsync(string entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _buffer.Add(entry);
        if (_buffer.Count >= _flushThreshold)
        {
            Flush();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        Flush();
        return ValueTask.CompletedTask;
    }

    private void Flush()
    {
        foreach (var entry in _buffer)
        {
            _sink.Add(entry);
        }

        _buffer.Clear();
    }
}
