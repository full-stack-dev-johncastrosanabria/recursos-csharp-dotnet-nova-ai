using System.Net;

namespace Training.Module08.Challenge;

/// <summary>
/// Retrying a failed call without retrying what must not be retried, and
/// without reusing an HttpRequestMessage that has already been sent.
/// </summary>
public sealed class RetryHandler(int maxAttempts) : DelegatingHandler
{
    public int MaxAttempts => maxAttempts;

    public int Attempts { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // A message can only be sent once, so every attempt after the
            // first needs its own.
            var outgoing = attempt == 1 ? request : Clone(request);

            response?.Dispose();
            Attempts++;
            response = await base.SendAsync(outgoing, cancellationToken);

            if (!ShouldRetry(request, response))
            {
                return response;
            }
        }

        return response!;
    }

    private static bool ShouldRetry(HttpRequestMessage request, HttpResponseMessage response)
    {
        // Safe methods only: repeating one cannot change anything at the far end.
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            return false;
        }

        return (int)response.StatusCode >= 500
            || response.StatusCode == HttpStatusCode.RequestTimeout;
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
