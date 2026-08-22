using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Training.Module07.Challenge;

/// <summary>Records what each request found already sitting in the middleware.</summary>
public sealed class StateLog
{
    public IList<string> FoundOnEntry { get; } = [];
}

/// <summary>
/// The bug. A conventional middleware is a single instance shared by every
/// request, so a field on it is application state wearing the costume of
/// request state.
/// </summary>
public sealed class FieldStateMiddleware(RequestDelegate next, StateLog log)
{
    private string _path = string.Empty;

    public Task InvokeAsync(HttpContext context)
    {
        log.FoundOnEntry.Add(_path);
        _path = context.Request.Path;

        return next(context);
    }
}

/// <summary>
/// The repair. HttpContext.Items lives and dies with the request, so nothing
/// survives into the next one.
/// </summary>
public sealed class ContextItemsMiddleware(RequestDelegate next, StateLog log)
{
    public Task InvokeAsync(HttpContext context)
    {
        log.FoundOnEntry.Add(context.Items[PerRequestState.ItemKey] as string ?? string.Empty);
        context.Items[PerRequestState.ItemKey] = context.Request.Path.ToString();

        return next(context);
    }
}

/// <summary>Registration and the two pipelines.</summary>
public static class PerRequestState
{
    public const string ItemKey = "training.path";

    public static IServiceCollection Register(IServiceCollection services, StateLog log)
    {
        services.AddSingleton(log);

        return services;
    }

    public static void ConfigureFieldState(IApplicationBuilder app)
    {
        app.UseMiddleware<FieldStateMiddleware>();
        app.Run(context => context.Response.WriteAsync("done"));
    }

    public static void ConfigureContextItems(IApplicationBuilder app)
    {
        app.UseMiddleware<ContextItemsMiddleware>();
        app.Run(context => context.Response.WriteAsync("done"));
    }
}
