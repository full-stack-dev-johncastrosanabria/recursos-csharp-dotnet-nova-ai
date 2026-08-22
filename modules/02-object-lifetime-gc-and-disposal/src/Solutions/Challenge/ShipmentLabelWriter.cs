using System.Text;

namespace Training.Module02.Challenge;

/// <summary>
/// Writes shipment labels to a stream, one per line.
///
/// The `leaveOpen` flag exists because ownership is not visible from the type
/// system: a Stream parameter looks identical whether the caller is handing it
/// over or lending it. Guessing wrong in one direction leaks a stream; guessing
/// wrong in the other closes one the caller is still writing to, and the
/// exception surfaces in their code rather than here.
/// </summary>
public sealed class ShipmentLabelWriter : IDisposable
{
    private readonly Stream _output;
    private readonly bool _leaveOpen;
    private bool _disposed;

    public ShipmentLabelWriter(Stream output, bool leaveOpen)
    {
        _output = output;
        _leaveOpen = leaveOpen;
    }

    public void Write(string label)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytes = Encoding.UTF8.GetBytes(label + "\n");
        _output.Write(bytes, 0, bytes.Length);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _output.Flush();

        if (!_leaveOpen)
        {
            _output.Dispose();
        }
    }
}
