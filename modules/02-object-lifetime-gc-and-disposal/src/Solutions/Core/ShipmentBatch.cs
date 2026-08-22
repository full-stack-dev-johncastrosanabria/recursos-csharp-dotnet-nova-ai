namespace Training.Module02.Core;

/// <summary>
/// Owns several resources acquired while preparing a shipment.
///
/// Two decisions worth naming. Reverse order, because a resource acquired later
/// may depend on an earlier one still being open. And a try/catch per child,
/// because the obvious foreach lets the first exception escape and leaks every
/// resource after it — the failure that was meant to be reported becomes a
/// second, larger leak.
/// </summary>
public sealed class ShipmentBatch : IDisposable
{
    private readonly List<IDisposable> _owned = [];
    private bool _disposed;

    public void Add(IDisposable resource)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _owned.Add(resource);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        List<Exception>? failures = null;

        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            try
            {
                _owned[i].Dispose();
            }
            catch (Exception error)
            {
                failures ??= [];
                failures.Add(error);
            }
        }

        _owned.Clear();

        if (failures is not null)
        {
            throw new AggregateException("One or more resources failed to close.", failures);
        }
    }
}
