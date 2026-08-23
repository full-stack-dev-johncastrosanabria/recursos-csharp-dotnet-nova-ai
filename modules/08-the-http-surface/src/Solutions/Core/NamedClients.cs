using Microsoft.Extensions.DependencyInjection;

namespace Training.Module08.Core;

/// <summary>
/// Letting the factory own the handlers: pooled, and rotated so a connection
/// cannot pin a DNS answer forever.
/// </summary>
public static class NamedClients
{
    public const string GatewayName = "gateway";

    public const string TenantHeader = "X-Tenant";

    public static IServiceCollection Register(IServiceCollection services, HttpMessageHandler primary)
    {
        services.AddHttpClient(GatewayName, client =>
            {
                client.BaseAddress = new Uri("https://gateway.invalid/");
                client.DefaultRequestHeaders.Add(TenantHeader, "acme");
            })
            .ConfigurePrimaryHttpMessageHandler(() => primary);

        return services;
    }

    public static HttpClient CreateGateway(IServiceProvider provider)
        => CreateByName(provider, GatewayName);

    public static HttpClient CreateByName(IServiceProvider provider, string name)
        => provider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
}
