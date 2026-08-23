namespace Training.Module08.Core;

/// <summary>What a gateway call produced, success or not.</summary>
public sealed record GatewayResult(bool Success, int StatusCode, string Body);

/// <summary>
/// A non-2xx is not an exception, and deciding to make it one is a decision
/// with a cost.
/// </summary>
public static class ResponseHandling
{
    public static async Task<GatewayResult> ReadAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new GatewayResult((int)response.StatusCode is >= 200 and <= 299, (int)response.StatusCode, body);
    }

    public static async Task<string> ReadOrThrowAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The body first. It is usually the only thing that says WHY.
            throw new HttpRequestException(
                $"{(int)response.StatusCode}: {body}", inner: null, response.StatusCode);
        }

        return body;
    }

    public static async Task<string> ReadWithEnsureAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);

        // Convenient, and it discards the body -- so the diagnostic that the
        // server went to the trouble of writing never reaches your logs.
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
