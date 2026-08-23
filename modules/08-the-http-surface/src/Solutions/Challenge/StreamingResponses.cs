namespace Training.Module08.Challenge;

/// <summary>
/// Reading a response as a stream rather than as a buffer, so peak memory is
/// the size of one chunk instead of the size of the response.
/// </summary>
public static class StreamingResponses
{
    public const int BufferSize = 4096;

    public static async Task<long> DownloadBufferedAsync(
        HttpClient client,
        string requestUri,
        Action onResponseReceived,
        CancellationToken cancellationToken)
    {
        // The default: this does not return until the whole body is in memory.
        using var response = await client.GetAsync(requestUri, cancellationToken);
        onResponseReceived();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return bytes.LongLength;
    }

    public static async Task<long> DownloadStreamedAsync(
        HttpClient client,
        string requestUri,
        Action onResponseReceived,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        onResponseReceived();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[BufferSize];
        long total = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
        }

        return total;
    }
}
