// The module's real-world case. A gate middleware reads endpoint metadata to
// decide whether a request needs an API key. It is correct, it is tested, and
// it is registered one line too early -- before UseRouting, which is what
// selects the endpoint in the first place.
//
// Before routing there is no endpoint. GetEndpoint() returns null, the gate
// finds no metadata, and it concludes the request needs no key. The protected
// endpoint answers everybody, with a 200.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("GET /admin with no API key. The endpoint requires one.");
Console.WriteLine();
Console.WriteLine($"  {"gate position",-28}{"status",8}{"body",10}");
Console.WriteLine("  " + new string('-', 46));

await Run("before UseRouting", app =>
{
    UseApiKeyGate(app);
    app.UseRouting();
    app.UseEndpoints(Map);
});

await Run("after UseRouting", app =>
{
    app.UseRouting();
    UseApiKeyGate(app);
    app.UseEndpoints(Map);
});

Console.WriteLine();
Console.WriteLine("The same middleware, the same endpoint, the same metadata. The only");
Console.WriteLine("difference is which line comes first, and it is the difference between an");
Console.WriteLine("admin endpoint that is closed and one that is open to the internet.");
Console.WriteLine();
Console.WriteLine("Note what does NOT happen in the broken row:");
Console.WriteLine();
Console.WriteLine("  No exception. The gate ran, asked its question, and got an answer.");
Console.WriteLine("  It just asked before there was anything to ask about.");
Console.WriteLine();
Console.WriteLine("  No log line. Nothing is wrong from the pipeline's point of view --");
Console.WriteLine("  a request arrived and a 200 went back.");
Console.WriteLine();
Console.WriteLine("  No failing test, unless a test sends an unauthenticated request to a");
Console.WriteLine("  protected route and asserts it is REJECTED. Tests that check the happy");
Console.WriteLine("  path with a valid key pass in both configurations.");
Console.WriteLine();
Console.WriteLine("The framework does guard its own metadata. Registering UseAuthorization");
Console.WriteLine("before UseRouting fails loudly rather than silently:");
Console.WriteLine();
await ShowFrameworkGuard();
Console.WriteLine();
Console.WriteLine("That check exists because this exact bug shipped, repeatedly, when the");
Console.WriteLine("ordering was newly introduced. It keys on the framework's own");
Console.WriteLine("IAuthorizeData, so it covers [Authorize] and RequireAuthorization -- and");
Console.WriteLine("nothing you wrote. A gate keyed on your own metadata gets no tripwire.");

static IApplicationBuilder UseApiKeyGate(IApplicationBuilder app)
    => app.Use(next => context =>
    {
        var needsKey = context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKey>() is not null;

        if (needsKey && !context.Request.Headers.ContainsKey("X-Api-Key"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return next(context);
    });

static void Map(IEndpointRouteBuilder endpoints)
    => endpoints.MapGet("/admin", context => context.Response.WriteAsync("SECRET"))
        .WithMetadata(new RequireApiKey());

static async Task Run(string label, Action<IApplicationBuilder> configure)
{
    using var provider = BuildProvider(services => { });
    var app = new ApplicationBuilder(provider);
    configure(app);
    var pipeline = app.Build();

    var context = new DefaultHttpContext { RequestServices = provider };
    context.Request.Method = "GET";
    context.Request.Path = "/admin";
    using var body = new MemoryStream();
    context.Response.Body = body;

    await pipeline(context);

    body.Position = 0;
    using var reader = new StreamReader(body);
    var text = await reader.ReadToEndAsync();

    Console.WriteLine($"  {label,-28}{context.Response.StatusCode,8}{(text.Length == 0 ? "-" : text),10}");
}

static async Task ShowFrameworkGuard()
{
    using var provider = BuildProvider(services => services.AddAuthorization());
    var app = new ApplicationBuilder(provider);
    app.UseAuthorization();
    app.UseRouting();
    app.UseEndpoints(endpoints =>
        endpoints.MapGet("/admin", context => context.Response.WriteAsync("SECRET"))
            .RequireAuthorization());

    var context = new DefaultHttpContext { RequestServices = provider };
    context.Request.Method = "GET";
    context.Request.Path = "/admin";
    context.Response.Body = Stream.Null;

    try
    {
        await app.Build()(context);
        Console.WriteLine($"    answered {context.Response.StatusCode} -- no complaint");
    }
    catch (InvalidOperationException error)
    {
        // The message arrives pre-wrapped; flatten it so the indentation holds.
        var flattened = error.Message.ReplaceLineEndings(" ");
        foreach (var sentence in flattened.Split(". ", StringSplitOptions.RemoveEmptyEntries))
        {
            Console.WriteLine($"    {sentence.Trim().TrimEnd('.')}.");
        }
    }
}

static ServiceProvider BuildProvider(Action<IServiceCollection> extra)
{
    var services = new ServiceCollection();
    var listener = new DiagnosticListener("Training.Module07");
    services.AddSingleton(listener);
    services.AddSingleton<DiagnosticSource>(listener);
    services.AddLogging();
    services.AddRouting();
    extra(services);

    return services.BuildServiceProvider();
}

/// <summary>The team's own metadata -- which is exactly why nothing warns about it.</summary>
internal sealed class RequireApiKey;
