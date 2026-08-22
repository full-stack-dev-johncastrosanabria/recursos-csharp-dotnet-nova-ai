namespace Training.Module02.Core;

/// <summary>
/// Buffers ledger entries and writes them out in batches.
///
/// Exercise: implement IAsyncDisposable. Entries below the flush threshold are
/// still in the buffer when the writer goes away, and if disposal does not
/// flush them they are lost silently — the caller saw every WriteAsync succeed.
/// Disposing twice must not write them twice.
/// </summary>
public sealed class LedgerWriter : IAsyncDisposable
{
    private readonly IList<string> _sink;
    private readonly int _flushThreshold;

    public LedgerWriter(IList<string> sink, int flushThreshold)
    {
        _sink = sink;
        _flushThreshold = flushThreshold;
    }

    public int Pending => throw new NotImplementedException();

    public ValueTask WriteAsync(string entry) => throw new NotImplementedException();

    public ValueTask DisposeAsync() => throw new NotImplementedException();
}
