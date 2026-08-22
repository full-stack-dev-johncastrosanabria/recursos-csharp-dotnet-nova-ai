using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Training.Module05.Core;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateways";

    public string Endpoint { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configures several gateways of the same shape under different names.
///
/// Named options exist for exactly this: one options type, many configured
/// instances, resolved by name through IOptionsMonitor&lt;T&gt;.Get(name) or
/// IOptionsSnapshot&lt;T&gt;.Get(name). Note that plain IOptions&lt;T&gt; has no Get --
/// it only ever sees the unnamed instance, which is why injecting it into a
/// multi-gateway service silently gives you defaults.
///
/// The sharp edge is that an unconfigured name is not an error. Ask for a name
/// that does not exist and the factory happily builds one from defaults, so a
/// typo becomes a client pointed at an empty endpoint rather than a startup
/// failure. If names come from configuration, validate them at startup.
/// </summary>
public static class NamedGatewayOptions
{
    public static IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        foreach (var gateway in configuration.GetSection(GatewayOptions.SectionName).GetChildren())
        {
            services.Configure<GatewayOptions>(gateway.Key, gateway);
        }

        return services;
    }

    public static IReadOnlyList<string> ConfiguredNames(IServiceProvider provider)
        =>
        [
            .. provider.GetRequiredService<IConfiguration>()
                .GetSection(GatewayOptions.SectionName)
                .GetChildren()
                .Select(gateway => gateway.Key),
        ];
}
