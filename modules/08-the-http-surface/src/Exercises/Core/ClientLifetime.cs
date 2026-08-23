namespace Training.Module08.Core;

/// <summary>
/// A primary handler that reports whether it is still alive.
///
/// The pool belongs to the handler, not to the client on top of it. That one
/// fact is what this exercise exists to make concrete.
/// </summary>
public sealed class TrackingPrimaryHandler : HttpMessageHandler
{
    public bool Disposed { get; private set; }

    public int SendCount { get; private set; }

    /// <summary>
    /// Exercise: refuse with ObjectDisposedException once this handler has been
    /// disposed -- which is what a real handler does, because the connection
    /// pool it needs is gone. Otherwise count the send and answer 200 "ok".
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    // Given rather than asked for: this part is plumbing. What is worth
    // noticing is WHERE it sits. Dispose(bool) on the handler is where a real
    // connection pool is torn down -- not on the HttpClient above it.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disposed = true;
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Exercise: two clients over one handler, with opposite ownership.
///
/// HttpClient is a thin, thread-safe facade. The connection pool lives in the
/// handler underneath it. That single fact explains both halves of this
/// module's real-world case: a client per request builds a pool per request
/// and never reuses a connection, and disposing a client destroys the pool the
/// next one will have to rebuild.
///
/// CreateOwning returns a client that disposes the handler with itself.
/// CreateBorrowing returns one that leaves the handler alone, which is what you
/// need when several clients share a pool. Both set BaseAddress to
/// https://gateway.invalid/.
/// </summary>
public static class ClientLifetime
{
    public static HttpClient CreateOwning(HttpMessageHandler handler)
        => throw new NotImplementedException();

    public static HttpClient CreateBorrowing(HttpMessageHandler handler)
        => throw new NotImplementedException();
}
