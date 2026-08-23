
namespace Training.Module08.Core;

/// <summary>Records which handler ran, on the way out and on the way back.</summary>
public sealed class HandlerLog
{
    public IList<string> Entries { get; } = [];
}

/// <summary>
/// A DelegatingHandler is outbound middleware. Module 07's pipeline, pointing
/// the other way: it does something, calls the inner handler, and does
/// something with the response.
///
/// Exercise: log "out:{Name}" before calling the inner handler and
/// "in:{Name}" after it returns, then return the response.
/// </summary>
public sealed class TracingHandler : DelegatingHandler
{
    public TracingHandler(string name, HandlerLog log)
    {
    }

    public string Name => throw new NotImplementedException();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}

/// <summary>
/// Exercise: link a list of delegating handlers into a chain ending at the
/// primary handler, and hand the result to an HttpClient.
///
/// The chain is built by setting each handler's InnerHandler, and -- exactly
/// as in module 07 -- it is built backwards, because a handler can only be
/// given its inner one once that inner one exists. The first handler in the
/// list is the outermost: it sees the request first and the response last.
///
/// Compose returns the outermost handler. CreateClient wraps it in an
/// HttpClient with BaseAddress set to https://gateway.invalid/ so tests can
/// send relative requests.
/// </summary>
public static class MessageHandlers
{
    public static HttpMessageHandler Compose(
        IReadOnlyList<DelegatingHandler> handlers,
        HttpMessageHandler primary)
        => throw new NotImplementedException();

    public static HttpClient CreateClient(
        IReadOnlyList<DelegatingHandler> handlers,
        HttpMessageHandler primary)
        => throw new NotImplementedException();
}
