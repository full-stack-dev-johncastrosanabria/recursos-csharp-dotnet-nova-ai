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
/// Exercise: bind each child of the Gateways section as a named options
/// instance, so Get("primary") and Get("fallback") resolve independently.
///
/// Worth knowing before you rely on it: an unconfigured name is not an error.
/// Ask for a name that does not exist and you get a fully-constructed object
/// full of defaults, so a typo becomes a client politely pointed at nothing.
/// </summary>
public static class NamedGatewayOptions
{
    public static IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
        => throw new NotImplementedException();

    public static IReadOnlyList<string> ConfiguredNames(IServiceProvider provider)
        => throw new NotImplementedException();
}
