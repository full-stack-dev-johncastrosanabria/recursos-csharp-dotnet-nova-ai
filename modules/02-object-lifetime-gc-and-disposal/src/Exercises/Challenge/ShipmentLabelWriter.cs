namespace Training.Module02.Challenge;

/// <summary>
/// Writes shipment labels to a stream, one per line.
///
/// Challenge: respect ownership. `leaveOpen: false` means this writer owns the
/// stream and closes it; `leaveOpen: true` means it was lent the stream and
/// must leave it open. Disposing a stream you were only lent breaks the caller
/// who is still using it, and it is the caller who gets the exception, far from
/// the code that caused it. This is why StreamWriter has the same flag.
/// </summary>
public sealed class ShipmentLabelWriter : IDisposable
{
    private readonly Stream _output;
    private readonly bool _leaveOpen;

    public ShipmentLabelWriter(Stream output, bool leaveOpen)
    {
        _output = output;
        _leaveOpen = leaveOpen;
    }

    public void Write(string label) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
