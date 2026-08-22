using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Training.Module05.Core;

namespace Training.Module05.Tests.Core;

public sealed class NamedGatewayOptionsTests
{
    private static ServiceProvider Build()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateways:primary:Endpoint"] = "https://primary.example",
                ["Gateways:primary:TimeoutSeconds"] = "5",
                ["Gateways:fallback:Endpoint"] = "https://fallback.example",
            })
            .Build();

        var services = new ServiceCollection();
        NamedGatewayOptions.Register(services, configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Each_name_resolves_its_own_configuration()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

        factory.Get("primary").Endpoint.ShouldBe("https://primary.example");
        factory.Get("fallback").Endpoint.ShouldBe("https://fallback.example");
    }

    [Fact]
    public void A_name_keeps_the_type_defaults_for_values_it_does_not_set()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

        factory.Get("fallback").TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void Names_do_not_leak_into_one_another()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

        factory.Get("primary").TimeoutSeconds.ShouldBe(5);
        factory.Get("fallback").TimeoutSeconds.ShouldNotBe(5);
    }

    [Fact]
    public void An_unconfigured_name_yields_defaults_rather_than_throwing()
    {
        // Worth knowing before you rely on it: a typo in a name is not an
        // error, it is a silently default-configured client pointed at nothing.
        using var provider = Build();
        var factory = provider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

        var typo = factory.Get("primry");

        typo.Endpoint.ShouldBe("");
        typo.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void The_configured_names_are_discoverable()
    {
        using var provider = Build();

        NamedGatewayOptions.ConfiguredNames(provider).Order().ShouldBe(["fallback", "primary"]);
    }
}
