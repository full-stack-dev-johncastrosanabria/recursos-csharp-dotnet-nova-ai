namespace Training.Module02.Core;

/// <summary>
/// Owns several resources acquired while preparing a shipment, and releases
/// them together.
///
/// Exercise: dispose everything it owns, in the reverse of the order it was
/// acquired, and do not let one failure strand the rest. A naive foreach that
/// lets the first exception escape leaks every resource after it. Report the
/// failures rather than swallowing them.
/// </summary>
public sealed class ShipmentBatch : IDisposable
{
    public void Add(IDisposable resource) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
