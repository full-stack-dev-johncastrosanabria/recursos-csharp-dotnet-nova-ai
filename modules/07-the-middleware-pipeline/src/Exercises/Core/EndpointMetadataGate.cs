using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Training.Module07.Core;

/// <summary>
/// Marks an endpoint as requiring an API key. Deliberately the team's own
/// metadata type rather than the framework's IAuthorizeData -- which is what
/// makes the failure below silent. See the guide's real-world case.
/// </summary>
public sealed class RequireApiKey;

/// <summary>
/// The module's real-world case, as an exercise.
///
/// Endpoint metadata is attached to the endpoint, and the endpoint is not
/// selected until UseRouting runs. A middleware placed before UseRouting is
/// therefore asking an empty question: GetEndpoint() returns null, so a gate
/// keyed on metadata finds none and lets the request through.
///
/// Exercise:
///
/// UseApiKeyGate adds one middleware. If the matched endpoint carries
/// RequireApiKey metadata and the request has no HeaderName header, it sets 401
/// and stops. Otherwise it continues.
///
/// MapEndpoints maps GET /health returning "ok" with no metadata, and GET
/// /admin returning "SECRET" carrying RequireApiKey.
///
/// ConfigureGateBeforeRouting places the gate, then UseRouting, then
/// UseEndpoints(MapEndpoints). ConfigureGateAfterRouting places UseRouting,
/// then the gate, then the same endpoints. Only one of them protects anything.
/// </summary>
public static class EndpointMetadataGate
{
    public const string HeaderName = "X-Api-Key";

    public static IApplicationBuilder UseApiKeyGate(IApplicationBuilder app)
        => throw new NotImplementedException();

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
        => throw new NotImplementedException();

    public static void ConfigureGateBeforeRouting(IApplicationBuilder app)
        => throw new NotImplementedException();

    public static void ConfigureGateAfterRouting(IApplicationBuilder app)
        => throw new NotImplementedException();
}
