using Microsoft.Extensions.DependencyInjection;

namespace Training.Module08.Core;

/// <summary>
/// Exercise: let the factory own the handlers.
///
/// IHttpClientFactory exists to solve both halves of this module's real-world
/// case at once. It pools handlers, so a client per call no longer means a
/// connection pool per call; and it retires each handler on a rotation, so a
/// pooled connection cannot pin a DNS answer for the life of the process.
/// Asking it for a client is cheap and correct; the HttpClient it returns is a
/// facade you may throw away.
///
/// Register configures a named client called GatewayName with BaseAddress
/// https://gateway.invalid/ and a default TenantHeader of "acme", over the
/// supplied primary handler.
///
/// CreateGateway asks the factory for that client. CreateByName asks for any
/// name -- which is how you meet the trap in the tests.
/// </summary>
public static class NamedClients
{
    public const string GatewayName = "gateway";

    public const string TenantHeader = "X-Tenant";

    public static IServiceCollection Register(IServiceCollection services, HttpMessageHandler primary)
        => throw new NotImplementedException();

    public static HttpClient CreateGateway(IServiceProvider provider)
        => throw new NotImplementedException();

    public static HttpClient CreateByName(IServiceProvider provider, string name)
        => throw new NotImplementedException();
}
