using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Training.Module07.Core;

/// <summary>
/// Three ways to send some requests down a different path, two of which never
/// come back.
/// </summary>
public static class PipelineBranching
{
    public const string BranchHeader = "X-Beta";

    public static void ConfigureMap(IApplicationBuilder app, IList<string> log)
    {
        app.Map("/api", branch => branch.Run(Branch(log)));
        app.Run(Main(log));
    }

    public static void ConfigureMapWhen(IApplicationBuilder app, IList<string> log)
    {
        app.MapWhen(
            context => context.Request.Headers.ContainsKey(BranchHeader),
            branch => branch.Run(Branch(log)));
        app.Run(Main(log));
    }

    public static void ConfigureUseWhen(IApplicationBuilder app, IList<string> log)
    {
        app.UseWhen(
            context => context.Request.Headers.ContainsKey(BranchHeader),
            branch => branch.Use(next => async context =>
            {
                log.Add("branch");
                await next(context);
            }));
        app.Run(Main(log));
    }

    private static RequestDelegate Branch(IList<string> log)
        => context =>
        {
            // Map rewrites the request: the matched segment moves to PathBase.
            log.Add($"branch:{context.Request.PathBase}|{context.Request.Path}");
            return context.Response.WriteAsync("api");
        };

    private static RequestDelegate Main(IList<string> log)
        => context =>
        {
            log.Add("main");
            return context.Response.WriteAsync("main");
        };
}
