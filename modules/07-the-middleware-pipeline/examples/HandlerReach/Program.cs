// Exception middleware is a try/catch around a call to next. That one sentence
// answers every question about where to put it.
//
// Whatever is registered BEHIND the handler is inside its try block. Whatever
// is registered in front of it is not, and fails past it into nothing. This is
// why the exception handler is the first thing registered in every ASP.NET Core
// template -- ahead of HTTPS redirection, static files, routing, all of it.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("One failing component. Three pipelines that differ only in where the");
Console.WriteLine("handler sits relative to it.");
Console.WriteLine();
Console.WriteLine($"  {"pipeline",-44}{"result",-24}");
Console.WriteLine("  " + new string('-', 68));

await Show("handler, then a component that throws", app =>
{
    app.Use(Handler);
    app.Use(Throwing("payment gateway"));
});

await Show("a component that throws, then handler", app =>
{
    app.Use(Throwing("payment gateway"));
    app.Use(Handler);
});

await Show("handler, two components, throwing terminal", app =>
{
    app.Use(Handler);
    app.Use(Passthrough);
    app.Use(Passthrough);
    app.Run(_ => throw new InvalidOperationException("payment gateway unreachable"));
});

Console.WriteLine();
Console.WriteLine("Depth does not matter -- row three is four layers down and still caught.");
Console.WriteLine("Only direction matters. The handler covers what it wraps.");
Console.WriteLine();
Console.WriteLine("The second row is the shape worth recognising in a real application. It");
Console.WriteLine("does not look like a missing handler, because there IS a handler and it");
Console.WriteLine("is right there in Program.cs. It simply sits behind the thing that");
Console.WriteLine("fails, so the failure never reaches it -- and what the client gets is");
Console.WriteLine("not a 500 page but a dropped connection, because nothing ever wrote a");
Console.WriteLine("response at all.");
Console.WriteLine();
Console.WriteLine("The corollary people miss: middleware registered before the handler is");
Console.WriteLine("unprotected by construction. If your logging or header middleware runs");
Console.WriteLine("first for good reasons, it also runs outside every safety net you have.");

static Task Handler(HttpContext context, RequestDelegate next) => Guard(context, next);

static Task Passthrough(HttpContext context, RequestDelegate next) => next(context);

static async Task Guard(HttpContext context, RequestDelegate next)
{
    try
    {
        await next(context);
    }
    catch (InvalidOperationException error)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        Outcome = $"caught: {error.Message}";
    }
}

static Func<HttpContext, RequestDelegate, Task> Throwing(string what)
    => (context, next) => throw new InvalidOperationException($"{what} unreachable");

static async Task Show(string label, Action<IApplicationBuilder> configure)
{
    Outcome = "completed";

    var services = new ServiceCollection();
    using var provider = services.BuildServiceProvider();
    var app = new ApplicationBuilder(provider);
    configure(app);

    var context = new DefaultHttpContext { RequestServices = provider };
    context.Response.Body = Stream.Null;

    try
    {
        await app.Build()(context);
    }
    catch (InvalidOperationException)
    {
        Outcome = "escaped the pipeline";
    }

    Console.WriteLine($"  {label,-44}{Outcome,-24}");
}

internal static partial class Program
{
    private static string Outcome { get; set; } = string.Empty;
}
