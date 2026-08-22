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
///
/// Challenge: on entry, record whatever the field currently holds into the log
/// (the empty string if it holds nothing), then store this request's path in
/// the field and call next.
///
/// Sequentially this leaks the previous request's data into the next. Under
/// concurrency it is worse and less predictable: two requests interleave and
/// one reads the other's value mid-flight. Either way the data crosses a
/// request boundary, which for a correlation id is confusing and for a user
/// id is a security incident.
/// </summary>
public sealed class FieldStateMiddleware
{
    public FieldStateMiddleware(RequestDelegate next, StateLog log)
    {
    }

    public Task InvokeAsync(HttpContext context) => throw new NotImplementedException();
}

/// <summary>
/// The repair. HttpContext.Items is a dictionary that lives and dies with the
/// request, so nothing survives into the next one.
///
/// Challenge: same behaviour, but read and write HttpContext.Items[ItemKey]
/// instead of a field.
/// </summary>
public sealed class ContextItemsMiddleware
{
    public ContextItemsMiddleware(RequestDelegate next, StateLog log)
    {
    }

    public Task InvokeAsync(HttpContext context) => throw new NotImplementedException();
}

/// <summary>
/// Exercise: register the log instance, then wire each middleware into its own
/// pipeline with UseMiddleware followed by a terminal Run writing "done".
/// </summary>
public static class PerRequestState
{
    public const string ItemKey = "training.path";

    public static IServiceCollection Register(IServiceCollection services, StateLog log)
        => throw new NotImplementedException();

    public static void ConfigureFieldState(IApplicationBuilder app)
        => throw new NotImplementedException();

    public static void ConfigureContextItems(IApplicationBuilder app)
        => throw new NotImplementedException();
}
