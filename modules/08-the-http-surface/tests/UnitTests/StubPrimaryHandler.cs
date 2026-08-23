using System.Net;

namespace Training.Module08.Tests;

/// <summary>
/// Stands in for the network at the bottom of a handler chain.
///
/// Every test in this module needs a primary handler that answers without a
/// socket: the exercises are about the client, the handler chain, timeouts and
/// header scope, none of which need a real server to be real. Where a test
/// genuinely needs sockets -- connection reuse -- that lives in examples/,
/// against a loopback listener, because a unit test that binds a port is a
/// flaky test.
/// </summary>
public sealed class StubPrimaryHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    public StubPrimaryHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "ok")
        : this((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body),
        }))
    {
    }

    public StubPrimaryHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        => _respond = respond;

    /// <summary>Every request this handler was asked to send, in order.</summary>
    public IList<HttpRequestMessage> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return await _respond(request, cancellationToken);
    }
}
