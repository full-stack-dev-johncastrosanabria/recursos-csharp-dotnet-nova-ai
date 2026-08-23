namespace Training.Module08.Challenge;

/// <summary>
/// Challenge: a header that belongs to one request must not live on the client.
///
/// The whole point of the previous exercises is that one HttpClient is shared
/// by everything. DefaultRequestHeaders belongs to that shared client, so a
/// header set there is set for every caller until somebody changes it -- and
/// for an Authorization header that means one user's credential attached to
/// another user's request.
///
/// It is module 07's per-request-state bug on the outbound side, and it is
/// worse here, because the shared state is a credential.
///
/// SendWithClientDefaultsAsync is the wrong version: when given a token it
/// replaces the client's default header, and when given none it sends as it
/// finds things. SendWithRequestHeaderAsync is the repair: the token goes on
/// the HttpRequestMessage, which exists for exactly one request, and the client
/// is never mutated.
/// </summary>
public static class HeaderScopes
{
    public const string TokenHeader = "Authorization";

    public static Task SendWithClientDefaultsAsync(
        HttpClient client,
        string requestUri,
        string? token,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public static Task SendWithRequestHeaderAsync(
        HttpClient client,
        string requestUri,
        string? token,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
