// Module 07's pipeline, pointing the other way.
//
// A DelegatingHandler chain is outbound middleware: each handler does something,
// calls the inner one, and does something with the response. Same onion, same
// rule that registration order is the only thing deciding it -- and on this side
// the ordering decides whether your retries are useful or actively harmful.

using System.Net;

Console.WriteLine("Two handlers, two orders, one expired token.");
Console.WriteLine();

await Show("retry OUTSIDE auth", ["retry", "auth"]);
Console.WriteLine();
await Show("retry INSIDE auth", ["auth", "retry"]);

Console.WriteLine();
Console.WriteLine("Read the indentation. In the first order the retry handler wraps the");
Console.WriteLine("auth handler, so every attempt re-enters auth and gets a fresh token --");
Console.WriteLine("the second attempt succeeds. In the second order auth runs once, on the");
Console.WriteLine("way in, and the retry handler underneath it replays the same expired");
Console.WriteLine("token as fast as it can.");
Console.WriteLine();
Console.WriteLine("That is the shape worth recognising: the retries are not merely useless,");
Console.WriteLine("they are three times the load on a dependency that is already rejecting");
Console.WriteLine("you, and every one of them will fail for the same reason. Neither order");
Console.WriteLine("is wrong in general. This one is wrong for these two handlers.");

static async Task Show(string label, string[] order)
{
    Console.WriteLine($"  {label}:");

    var tokens = new TokenSource();
    var handlers = order.Select(name => name == "auth"
        ? (DelegatingHandler)new AuthHandler(tokens)
        : new RetryHandler()).ToArray();

    var inner = (HttpMessageHandler)new GatewayHandler();
    for (var index = handlers.Length - 1; index >= 0; index--)
    {
        handlers[index].InnerHandler = inner;
        inner = handlers[index];
    }

    using var client = new HttpClient(inner) { BaseAddress = new Uri("https://gateway.invalid/") };
    var response = await client.GetAsync("orders");

    Console.WriteLine($"      result: {(int)response.StatusCode} after {Trace.Attempts} attempt(s)");
    Trace.Reset();
}

internal static class Trace
{
    public static int Depth { get; set; }

    public static int Attempts { get; set; }

    public static void Line(string message)
        => Console.WriteLine($"      {new string(' ', Depth * 4)}{message}");

    public static void Reset()
    {
        Depth = 0;
        Attempts = 0;
    }
}

/// <summary>Hands out a token that is expired the first time it is asked.</summary>
internal sealed class TokenSource
{
    private int _issued;

    public string Next() => ++_issued == 1 ? "expired" : "fresh";
}

internal sealed class AuthHandler(TokenSource tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = tokens.Next();
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", token);
        Trace.Line($"auth -> attaches {token} token");
        Trace.Depth++;

        var response = await base.SendAsync(request, cancellationToken);

        Trace.Depth--;
        Trace.Line($"auth <- {(int)response.StatusCode}");

        return response;
    }
}

internal sealed class RetryHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Trace.Line($"retry -> attempt {attempt}");
            Trace.Depth++;

            response?.Dispose();
            var outgoing = attempt == 1 ? request : Clone(request);
            response = await base.SendAsync(outgoing, cancellationToken);

            Trace.Depth--;
            Trace.Line($"retry <- {(int)response.StatusCode}");

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                break;
            }
        }

        return response!;
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

/// <summary>Accepts a fresh token and rejects an expired one.</summary>
internal sealed class GatewayHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Trace.Attempts++;
        var token = request.Headers.TryGetValues("Authorization", out var values)
            ? values.First()
            : "none";
        var status = token == "fresh" ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
        Trace.Line($"gateway: token={token} -> {(int)status}");

        return Task.FromResult(new HttpResponseMessage(status));
    }
}
