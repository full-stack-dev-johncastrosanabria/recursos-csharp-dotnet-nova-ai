using System.Net;

namespace Training.Module08.Tests;

/// <summary>
/// Response content that reports how much of itself has actually been written.
///
/// That is the only way to see the difference streaming makes from the outside:
/// with the default completion option the whole body is written before the send
/// returns, and with ResponseHeadersRead none of it is.
/// </summary>
public sealed class ProgressContent(int chunks, int chunkSize) : HttpContent
{
    public long BytesWritten { get; private set; }

    public long Total => (long)chunks * chunkSize;

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var chunk = new byte[chunkSize];

        for (var index = 0; index < chunks; index++)
        {
            await stream.WriteAsync(chunk);
            BytesWritten += chunkSize;
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = Total;

        return true;
    }
}
