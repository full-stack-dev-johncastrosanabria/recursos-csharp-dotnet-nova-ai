namespace Training.Module08.Core;

/// <summary>A primary handler that reports whether it is still alive.</summary>
public sealed class TrackingPrimaryHandler : HttpMessageHandler
{
    public bool Disposed { get; private set; }

    public int SendCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // A real handler cannot serve a request once its pool is gone.
        ObjectDisposedException.ThrowIf(Disposed, this);
        SendCount++;

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("ok"),
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Where a real handler's connection pool is torn down.
            Disposed = true;
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Two clients over one handler, with opposite ownership. HttpClient is a thin
/// facade; the connection pool lives in the handler underneath it.
/// </summary>
public static class ClientLifetime
{
    public static HttpClient CreateOwning(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
        };

    public static HttpClient CreateBorrowing(HttpMessageHandler handler)
        => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
        };
}
