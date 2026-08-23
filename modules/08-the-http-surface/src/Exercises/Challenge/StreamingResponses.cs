namespace Training.Module08.Challenge;

/// <summary>
/// Challenge: stop reading whole responses into memory before you look at them.
///
/// By default HttpClient does not return until the entire response body has
/// been read into a buffer. That is convenient and almost always what you want
/// for a small JSON payload. For anything large it means peak memory equal to
/// the response size, per concurrent request -- which is why a report endpoint
/// that works all year fails in January.
///
/// HttpCompletionOption.ResponseHeadersRead returns as soon as the status and
/// headers have arrived, leaving the body to be read as a stream.
///
/// Both methods return the total number of bytes in the body, and both invoke
/// onResponseReceived at the moment the send completes and before the body is
/// consumed -- which is where the tests look to tell the two apart.
///
/// DownloadBufferedAsync uses the default. DownloadStreamedAsync asks for
/// headers only and then reads the stream in fixed-size chunks, so its peak
/// memory is the size of one buffer rather than the size of the response.
/// </summary>
public static class StreamingResponses
{
    public const int BufferSize = 4096;

    public static Task<long> DownloadBufferedAsync(
        HttpClient client,
        string requestUri,
        Action onResponseReceived,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public static Task<long> DownloadStreamedAsync(
        HttpClient client,
        string requestUri,
        Action onResponseReceived,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
