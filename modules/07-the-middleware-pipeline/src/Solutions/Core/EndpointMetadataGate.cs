using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Training.Module07.Core;

/// <summary>
/// Marks an endpoint as requiring an API key. Deliberately the team's own
/// metadata type rather than the framework's IAuthorizeData -- which is what
/// makes the failure below silent. See the guide's real-world case.
/// </summary>
public sealed class RequireApiKey;

/// <summary>
/// The module's real-world case.
///
/// Endpoint metadata is attached to the endpoint, and the endpoint is not
/// selected until UseRouting runs. A middleware placed before UseRouting is
/// therefore asking an empty question: GetEndpoint() returns null, so a gate
/// keyed on metadata finds none and lets the request through.
/// </summary>
public static class EndpointMetadataGate
{
    public const string HeaderName = "X-Api-Key";

    public static IApplicationBuilder UseApiKeyGate(IApplicationBuilder app)
        => app.Use(next => context =>
        {
            var protectedEndpoint =
                context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKey>() is not null;

            if (protectedEndpoint && !context.Request.Headers.ContainsKey(HeaderName))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            return next(context);
        });

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", context => context.Response.WriteAsync("ok"));
        endpoints.MapGet("/admin", context => context.Response.WriteAsync("SECRET"))
            .WithMetadata(new RequireApiKey());
    }

    /// <summary>The bug. The gate runs before an endpoint has been selected.</summary>
    public static void ConfigureGateBeforeRouting(IApplicationBuilder app)
    {
        UseApiKeyGate(app);
        app.UseRouting();
        app.UseEndpoints(MapEndpoints);
    }

    /// <summary>The repair, and the only difference is which line comes first.</summary>
    public static void ConfigureGateAfterRouting(IApplicationBuilder app)
    {
        app.UseRouting();
        UseApiKeyGate(app);
        app.UseEndpoints(MapEndpoints);
    }
}
