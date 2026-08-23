namespace Training.Module08.Challenge;

/// <summary>
/// A header that belongs to one request must not live on the client, because
/// the client is shared by every caller.
/// </summary>
public static class HeaderScopes
{
    public const string TokenHeader = "Authorization";

    public static async Task SendWithClientDefaultsAsync(
        HttpClient client,
        string requestUri,
        string? token,
        CancellationToken cancellationToken)
    {
        if (token is not null)
        {
            // Shared, mutable, and outliving this call.
            client.DefaultRequestHeaders.Remove(TokenHeader);
            client.DefaultRequestHeaders.Add(TokenHeader, token);
        }

        using var response = await client.GetAsync(requestUri, cancellationToken);
    }

    public static async Task SendWithRequestHeaderAsync(
        HttpClient client,
        string requestUri,
        string? token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        if (token is not null)
        {
            request.Headers.Add(TokenHeader, token);
        }

        using var response = await client.SendAsync(request, cancellationToken);
    }
}
