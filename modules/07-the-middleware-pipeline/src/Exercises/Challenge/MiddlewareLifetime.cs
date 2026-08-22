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
/// The bug, and it is module 06's captive dependency in a new place.
///
/// A conventional middleware class is constructed ONCE, when the pipeline is
/// built, from the application's root provider. A scoped service taken in its
/// constructor is therefore resolved once, from the root scope, and held for
/// the life of the application -- every request sees the same one.
///
/// Challenge: InvokeAsync records the constructor-injected marker's id in the
/// log and calls next.
/// </summary>
public sealed class CapturingMiddleware
{
    public CapturingMiddleware(RequestDelegate next, RequestScopedMarker marker, MarkerLog log)
    {
    }

    public Task InvokeAsync(HttpContext context) => throw new NotImplementedException();
}

/// <summary>
/// The repair. Scoped services arrive as extra InvokeAsync parameters, which
/// UseMiddleware resolves from the request's own scope every time.
///
/// Challenge: InvokeAsync records the marker it was handed and calls next.
/// </summary>
public sealed class PerRequestMiddleware
{
    public PerRequestMiddleware(RequestDelegate next, MarkerLog log)
    {
    }

    public Task InvokeAsync(HttpContext context, RequestScopedMarker marker)
        => throw new NotImplementedException();
}

/// <summary>
/// Exercise: register the marker as scoped and the log as the given instance,
/// then wire each of the two middleware classes into its own pipeline with
/// UseMiddleware, followed by a terminal Run that writes "done".
/// </summary>
public static class MiddlewareLifetime
{
    public static IServiceCollection Register(IServiceCollection services, MarkerLog log)
        => throw new NotImplementedException();

    public static void ConfigureCapturing(IApplicationBuilder app)
        => throw new NotImplementedException();

    public static void ConfigurePerRequest(IApplicationBuilder app)
        => throw new NotImplementedException();
}
