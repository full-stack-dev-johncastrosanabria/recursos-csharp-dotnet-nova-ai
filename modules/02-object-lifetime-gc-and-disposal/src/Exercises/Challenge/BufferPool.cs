namespace Training.Module02.Challenge;

/// <summary>
/// Hands out reusable byte buffers so a hot path does not allocate one per call.
///
/// Challenge: pooling moves the cost from the collector to you, and you now own
/// the bugs the collector used to prevent. Returning a buffer twice hands the
/// same array to two callers who both believe they own it — the managed
/// equivalent of a double free. Returning a buffer this pool never issued is
/// just as wrong. Detect both, and clear a returned buffer so one caller's data
/// does not surface in another's.
/// </summary>
public sealed class BufferPool
{
    public BufferPool(int bufferSize) => BufferSize = bufferSize;

    public int BufferSize { get; }

    public int Available => throw new NotImplementedException();

    public int Rented => throw new NotImplementedException();

    public byte[] Rent() => throw new NotImplementedException();

    public void Return(byte[] buffer) => throw new NotImplementedException();
}
