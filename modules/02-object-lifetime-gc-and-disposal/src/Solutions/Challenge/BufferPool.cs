namespace Training.Module02.Challenge;

/// <summary>
/// Hands out reusable byte buffers.
///
/// Two sets, because two different questions are being asked. `_issued` is
/// every buffer this pool has ever created, which answers "is this mine". `_out`
/// is the subset currently in someone's hands, which answers "is this already
/// back". Collapsing them into one set makes a double-return indistinguishable
/// from a legitimate return.
///
/// Arrays do not override Equals, so a HashSet of them compares by reference,
/// which is exactly what identity tracking needs here.
/// </summary>
public sealed class BufferPool
{
    private readonly HashSet<byte[]> _issued = [];
    private readonly HashSet<byte[]> _out = [];
    private readonly Stack<byte[]> _idle = new();

    public BufferPool(int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 1);
        BufferSize = bufferSize;
    }

    public int BufferSize { get; }

    public int Available => _idle.Count;

    public int Rented => _out.Count;

    public byte[] Rent()
    {
        byte[] buffer;

        if (_idle.Count > 0)
        {
            buffer = _idle.Pop();
        }
        else
        {
            buffer = new byte[BufferSize];
            _issued.Add(buffer);
        }

        _out.Add(buffer);
        return buffer;
    }

    public void Return(byte[] buffer)
    {
        if (!_issued.Contains(buffer))
        {
            throw new InvalidOperationException(
                "That buffer did not come from this pool. Returning a foreign buffer corrupts the pool's accounting.");
        }

        if (!_out.Remove(buffer))
        {
            throw new InvalidOperationException(
                "That buffer has already been returned. Returning twice hands one array to two callers who both believe they own it.");
        }

        Array.Clear(buffer);
        _idle.Push(buffer);
    }
}
