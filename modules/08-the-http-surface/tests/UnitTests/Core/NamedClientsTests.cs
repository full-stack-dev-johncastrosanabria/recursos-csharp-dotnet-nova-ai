using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module08.Core;

namespace Training.Module08.Tests.Core;

public sealed class NamedClientsTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_named_client_arrives_configured()
    {
        using var primary = new StubPrimaryHandler();
        using var provider = Build(primary);

        var client = NamedClients.CreateGateway(provider);
        await client.GetAsync("orders", Token);

        client.BaseAddress.ShouldBe(new Uri("https://gateway.invalid/"));
        primary.Requests[0].Headers.GetValues(NamedClients.TenantHeader).ShouldBe(["acme"]);
    }

    [Fact]
    public void An_unknown_name_silently_returns_an_unconfigured_client()
    {
        // Module 05's named options, again: asking for a name nobody
        // registered is not an error. You get a real HttpClient with no base
        // address, and the first relative request fails somewhere else
        // entirely -- or worse, a configured absolute URL succeeds without the
        // headers you assumed were attached.
        using var primary = new StubPrimaryHandler();
        using var provider = Build(primary);

        var client = NamedClients.CreateByName(provider, "gatway");

        client.BaseAddress.ShouldBeNull();
        client.DefaultRequestHeaders.Contains(NamedClients.TenantHeader).ShouldBeFalse();
    }

    [Fact]
    public void Each_call_returns_a_new_facade()
    {
        using var primary = new StubPrimaryHandler();
        using var provider = Build(primary);

        var first = NamedClients.CreateGateway(provider);
        var second = NamedClients.CreateGateway(provider);

        first.ShouldNotBeSameAs(second);
    }

    [Fact]
    public async Task But_they_share_the_handler_underneath()
    {
        // Which is the entire point: the expensive thing is pooled even though
        // the cheap thing is not.
        using var primary = new StubPrimaryHandler();
        using var provider = Build(primary);

        await NamedClients.CreateGateway(provider).GetAsync("orders", Token);
        await NamedClients.CreateGateway(provider).GetAsync("orders", Token);

        primary.Requests.Count.ShouldBe(2);
    }

    private static ServiceProvider Build(HttpMessageHandler primary)
    {
        var services = new ServiceCollection();
        NamedClients.Register(services, primary);

        return services.BuildServiceProvider();
    }
}
