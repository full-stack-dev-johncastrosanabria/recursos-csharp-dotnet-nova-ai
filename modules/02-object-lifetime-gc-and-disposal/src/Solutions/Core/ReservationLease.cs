namespace Training.Module02.Core;

/// <summary>
/// A hold on stock for one order line.
///
/// The `_disposed` guard is the whole pattern: Dispose is idempotent because
/// callers genuinely cannot always tell whether it already ran, and every other
/// public member refuses to work afterwards rather than pretending to succeed.
/// </summary>
public sealed class ReservationLease : IDisposable
{
    private readonly Action<string, int> _release;
    private bool _disposed;
    private int _renewals;

    public ReservationLease(string sku, int quantity, Action<string, int> release)
    {
        Sku = sku;
        Quantity = quantity;
        _release = release;
    }

    public string Sku { get; }

    public int Quantity { get; }

    public bool IsDisposed => _disposed;

    public int Renewals => _renewals;

    public void Renew()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _renewals++;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _release(Sku, Quantity);
    }
}
