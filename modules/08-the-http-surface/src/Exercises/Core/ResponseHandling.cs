namespace Training.Module08.Core;

/// <summary>What a gateway call produced, success or not.</summary>
public sealed record GatewayResult(bool Success, int StatusCode, string Body);

/// <summary>
/// Exercise: a non-2xx is not an exception, and deciding to make it one is a
/// decision with a cost.
///
/// HttpClient does not throw on 404 or 503. Those are answers -- the request
/// reached the server and the server replied. Only a transport failure throws.
/// That surprises people coming from clients that throw on everything, and it
/// is the reason so much code checks nothing and treats an error page as data.
///
/// ReadAsync reports the outcome without throwing, capturing the body either
/// way. ReadOrThrowAsync throws on a non-2xx, but reads the body FIRST and puts
/// it in the message. ReadWithEnsureAsync uses EnsureSuccessStatusCode instead,
/// which is the convenient version and throws away the only part of the
/// response that would have told you what went wrong.
/// </summary>
public static class ResponseHandling
{
    public static Task<GatewayResult> ReadAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public static Task<string> ReadOrThrowAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public static Task<string> ReadWithEnsureAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
