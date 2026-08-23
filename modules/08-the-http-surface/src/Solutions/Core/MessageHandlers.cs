
namespace Training.Module08.Core;

/// <summary>Records which handler ran, on the way out and on the way back.</summary>
public sealed class HandlerLog
{
    public IList<string> Entries { get; } = [];
}

/// <summary>
/// A DelegatingHandler is outbound middleware -- module 07's pipeline pointing
/// the other way.
/// </summary>
public sealed class TracingHandler(string name, HandlerLog log) : DelegatingHandler
{
    public string Name => name;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        log.Entries.Add($"out:{name}");
        var response = await base.SendAsync(request, cancellationToken);
        log.Entries.Add($"in:{name}");

        return response;
    }
}

/// <summary>Linking handlers into a chain, and putting a client on top of it.</summary>
public static class MessageHandlers
{
    public static HttpMessageHandler Compose(
        IReadOnlyList<DelegatingHandler> handlers,
        HttpMessageHandler primary)
    {
        var inner = primary;

        // Backwards, for the same reason module 07's fold runs backwards: a
        // handler can only be given its inner one once that inner one exists.
        for (var index = handlers.Count - 1; index >= 0; index--)
        {
            handlers[index].InnerHandler = inner;
            inner = handlers[index];
        }

        return inner;
    }

    public static HttpClient CreateClient(
        IReadOnlyList<DelegatingHandler> handlers,
        HttpMessageHandler primary)
        => new(Compose(handlers, primary))
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
        };
}
