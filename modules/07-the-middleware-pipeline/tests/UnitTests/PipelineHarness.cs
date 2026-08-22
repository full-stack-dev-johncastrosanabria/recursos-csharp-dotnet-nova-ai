using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Training.Module07.Tests;

/// <summary>What one request through a pipeline produced.</summary>
public sealed record PipelineResult(int StatusCode, string Body, HttpContext Context);

/// <summary>
/// Builds a real ASP.NET Core pipeline in memory and sends one request through
/// it. No server, no socket, no Docker -- <see cref="ApplicationBuilder"/> and
/// <see cref="DefaultHttpContext"/> are the same types the web host uses, so
/// what these tests observe is the real middleware behaviour rather than a
/// simulation of it.
///
/// Anything a test needs to read after the request must be an object the test
/// created and registered itself. The harness disposes the provider on the way
/// out, and (as module 06 established) an instance you register is not the
/// container's to dispose -- so a recorder handed in here survives.
/// </summary>
public static class PipelineHarness
{
    public static async Task<PipelineResult> SendAsync(
        Action<IApplicationBuilder> configure,
        Action<IServiceCollection>? register = null,
        string path = "/",
        Action<HttpContext>? prepare = null)
    {
        var services = new ServiceCollection();

        // EndpointRoutingMiddleware takes a DiagnosticListener by constructor
        // injection. The web host registers one; a bare ServiceCollection does
        // not, so UseRouting would fail to activate without these two lines.
        var listener = new DiagnosticListener("Training.Module07");
        services.AddSingleton(listener);
        services.AddSingleton<DiagnosticSource>(listener);
        services.AddLogging();
        services.AddRouting();
        register?.Invoke(services);

        var provider = services.BuildServiceProvider();

        try
        {
            var app = new ApplicationBuilder(provider);
            configure(app);
            var pipeline = app.Build();

            return await SendOneAsync(pipeline, provider, path, prepare);
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>
    /// Several requests through ONE pipeline and one container -- each in its
    /// own scope, as a web host would. Needed wherever the question is whether
    /// something is shared across requests rather than within one.
    /// </summary>
    public static async Task<IReadOnlyList<PipelineResult>> SendManyAsync(
        Action<IApplicationBuilder> configure,
        Action<IServiceCollection>? register = null,
        params string[] paths)
    {
        var services = new ServiceCollection();
        var listener = new DiagnosticListener("Training.Module07");
        services.AddSingleton(listener);
        services.AddSingleton<DiagnosticSource>(listener);
        services.AddLogging();
        services.AddRouting();
        register?.Invoke(services);

        var provider = services.BuildServiceProvider();

        try
        {
            var app = new ApplicationBuilder(provider);
            configure(app);
            var pipeline = app.Build();

            var results = new List<PipelineResult>();
            foreach (var path in paths)
            {
                results.Add(await SendOneAsync(pipeline, provider, path, prepare: null));
            }

            return results;
        }
        finally
        {
            provider.Dispose();
        }
    }

    private static async Task<PipelineResult> SendOneAsync(
        RequestDelegate pipeline,
        IServiceProvider provider,
        string path,
        Action<HttpContext>? prepare)
    {
        // A scope per request, which is what the web host does and what makes
        // "scoped" mean "per request" at all -- see module 06.
        using var scope = provider.CreateScope();

        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = "GET";
        context.Request.Path = path;

        using var body = new MemoryStream();
        context.Response.Body = body;
        prepare?.Invoke(context);

        await pipeline(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        var text = await reader.ReadToEndAsync();

        return new PipelineResult(context.Response.StatusCode, text, context);
    }
}
