using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Training.Module07.Challenge;

/// <summary>Scoped, so one per request -- stands in for anything per-request.</summary>
public sealed class RequestScopedMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>Collects the marker id each request actually observed.</summary>
public sealed class MarkerLog
{
    public IList<Guid> Observed { get; } = [];
}

/// <summary>
/// The bug: module 06's captive dependency in a new place. A conventional
/// middleware class is constructed once, from the root provider, so a scoped
/// service in its constructor is captured for the life of the application.
/// </summary>
public sealed class CapturingMiddleware(RequestDelegate next, RequestScopedMarker marker, MarkerLog log)
{
    public Task InvokeAsync(HttpContext context)
    {
        log.Observed.Add(marker.Id);
        return next(context);
    }
}

/// <summary>
/// The repair. Scoped services arrive as extra InvokeAsync parameters, which
/// UseMiddleware resolves from the request's own scope every time.
/// </summary>
public sealed class PerRequestMiddleware(RequestDelegate next, MarkerLog log)
{
    public Task InvokeAsync(HttpContext context, RequestScopedMarker marker)
    {
        log.Observed.Add(marker.Id);
        return next(context);
    }
}

/// <summary>Registration and the two pipelines.</summary>
public static class MiddlewareLifetime
{
    public static IServiceCollection Register(IServiceCollection services, MarkerLog log)
    {
        services.AddSingleton(log);
        services.AddScoped<RequestScopedMarker>();

        return services;
    }

    public static void ConfigureCapturing(IApplicationBuilder app)
    {
        app.UseMiddleware<CapturingMiddleware>();
        app.Run(context => context.Response.WriteAsync("done"));
    }

    public static void ConfigurePerRequest(IApplicationBuilder app)
    {
        app.UseMiddleware<PerRequestMiddleware>();
        app.Run(context => context.Response.WriteAsync("done"));
    }
}
