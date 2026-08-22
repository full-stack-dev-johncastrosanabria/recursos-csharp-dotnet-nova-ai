using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Training.Module07.Core;

/// <summary>
/// Three pipelines that make the difference between continuing and stopping
/// visible.
/// </summary>
public static class ShortCircuiting
{
    public static void ConfigureWithTerminal(IApplicationBuilder app, IList<string> log)
    {
        app.Use(Recording(log, "one"));
        app.Use(Recording(log, "two"));
        app.Run(Terminal(log));
    }

    public static void ConfigureWithGuard(IApplicationBuilder app, IList<string> log)
    {
        app.Use(Recording(log, "one"));
        app.Use(next => context =>
        {
            log.Add("guard");
            context.Response.StatusCode = 403;

            // No call to next. Everything registered after this point is
            // simply not run -- there is no error and no signal upstream.
            return Task.CompletedTask;
        });
        app.Run(Terminal(log));
    }

    public static void ConfigureAfterTerminal(IApplicationBuilder app, IList<string> log)
    {
        app.Run(Terminal(log));

        // Registered, reachable by nothing. Run does not take a next delegate,
        // so the chain ends there and this component is never composed in.
        app.Use(Recording(log, "late"));
    }

    private static Func<RequestDelegate, RequestDelegate> Recording(IList<string> log, string name)
        => next => async context =>
        {
            log.Add($"in:{name}");
            await next(context);
            log.Add($"out:{name}");
        };

    private static RequestDelegate Terminal(IList<string> log)
        => context =>
        {
            log.Add("terminal");
            return context.Response.WriteAsync("handled");
        };
}
