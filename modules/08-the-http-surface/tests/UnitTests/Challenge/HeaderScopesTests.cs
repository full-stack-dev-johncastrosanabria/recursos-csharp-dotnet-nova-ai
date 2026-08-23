using Shouldly;
using Training.Module08.Challenge;

namespace Training.Module08.Tests.Challenge;

public sealed class HeaderScopesTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_client_default_header_outlives_the_caller_that_set_it()
    {
        // The incident. The second caller supplied no credential and their
        // request went out carrying the first caller's.
        using var primary = new StubPrimaryHandler();
        using var client = ClientOver(primary);

        await HeaderScopes.SendWithClientDefaultsAsync(client, "orders", "Bearer caller-a", Token);
        await HeaderScopes.SendWithClientDefaultsAsync(client, "orders", null, Token);

        HeaderOf(primary, 1).ShouldBe("Bearer caller-a");
    }

    [Fact]
    public async Task A_per_request_header_does_not()
    {
        using var primary = new StubPrimaryHandler();
        using var client = ClientOver(primary);

        await HeaderScopes.SendWithRequestHeaderAsync(client, "orders", "Bearer caller-a", Token);
        await HeaderScopes.SendWithRequestHeaderAsync(client, "orders", null, Token);

        HeaderOf(primary, 1).ShouldBeNull();
    }

    [Fact]
    public async Task Both_forms_look_correct_when_every_caller_supplies_a_token()
    {
        // Which is why this survives review and testing. Nothing is visibly
        // wrong until a caller omits the header.
        using var withDefaults = new StubPrimaryHandler();
        using var perRequest = new StubPrimaryHandler();
        using var first = ClientOver(withDefaults);
        using var second = ClientOver(perRequest);

        await HeaderScopes.SendWithClientDefaultsAsync(first, "orders", "Bearer a", Token);
        await HeaderScopes.SendWithClientDefaultsAsync(first, "orders", "Bearer b", Token);
        await HeaderScopes.SendWithRequestHeaderAsync(second, "orders", "Bearer a", Token);
        await HeaderScopes.SendWithRequestHeaderAsync(second, "orders", "Bearer b", Token);

        HeaderOf(withDefaults, 1).ShouldBe("Bearer b");
        HeaderOf(perRequest, 1).ShouldBe("Bearer b");
    }

    [Fact]
    public async Task The_per_request_form_never_touches_the_shared_client()
    {
        using var primary = new StubPrimaryHandler();
        using var client = ClientOver(primary);

        await HeaderScopes.SendWithRequestHeaderAsync(client, "orders", "Bearer caller-a", Token);

        client.DefaultRequestHeaders.Contains(HeaderScopes.TokenHeader).ShouldBeFalse();
    }

    [Fact]
    public async Task The_default_form_leaves_its_mark_on_the_client_itself()
    {
        using var primary = new StubPrimaryHandler();
        using var client = ClientOver(primary);

        await HeaderScopes.SendWithClientDefaultsAsync(client, "orders", "Bearer caller-a", Token);

        client.DefaultRequestHeaders.GetValues(HeaderScopes.TokenHeader).ShouldBe(["Bearer caller-a"]);
    }

    private static HttpClient ClientOver(HttpMessageHandler handler)
        => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://gateway.invalid/") };

    private static string? HeaderOf(StubPrimaryHandler primary, int index)
        => primary.Requests[index].Headers.TryGetValues(HeaderScopes.TokenHeader, out var values)
            ? string.Join(",", values)
            : null;
}
