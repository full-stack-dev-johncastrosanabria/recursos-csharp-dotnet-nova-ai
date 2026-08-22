// The pipeline is not a list of steps. It is a set of nested wrappers, and the
// nesting is what people get wrong.
//
// Each component runs some code, awaits the next one, and then runs more code.
// So registration order is the order on the way in and the REVERSE order on the
// way out -- which decides, for example, whether your timing middleware
// measures the handler or measures everything inside it.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Three components and a terminal handler, registered one two three.");
Console.WriteLine();

await Show("everything calls next", app =>
{
    app.Use(Tracing("one"));
    app.Use(Tracing("two"));
    app.Use(Tracing("three"));
    app.Run(context =>
    {
        Log("handler");
        return context.Response.WriteAsync("ok");
    });
});

Console.WriteLine();
Console.WriteLine("Read the indentation: three is inside two, which is inside one. The");
Console.WriteLine("handler is at the centre. Nothing about this is a queue.");
Console.WriteLine();

await Show("two refuses to continue", app =>
{
    app.Use(Tracing("one"));
    app.Use(next => context =>
    {
        Log("two SHORT-CIRCUITS");
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    });
    app.Use(Tracing("three"));
    app.Run(context =>
    {
        Log("handler");
        return context.Response.WriteAsync("ok");
    });
});

Console.WriteLine();
Console.WriteLine("Three and the handler never ran. One still finished, because one had");
Console.WriteLine("already called next and has no way to know what happened in there.");
Console.WriteLine();
Console.WriteLine("That asymmetry is worth holding on to. A middleware sees its own");
Console.WriteLine("beginning and end, and nothing about whether the request it forwarded");
Console.WriteLine("was answered, refused or abandoned. Logging middleware placed first");
Console.WriteLine("reports a completed request either way -- so a pipeline that quietly");
Console.WriteLine("rejects traffic looks, in the logs, exactly like one that serves it.");

static Func<RequestDelegate, RequestDelegate> Tracing(string name)
    => next => async context =>
    {
        Log($"{name} -> in");
        Depth++;
        await next(context);
        Depth--;
        Log($"{name} <- out");
    };

static void Log(string message) => Console.WriteLine($"  {new string(' ', Depth * 4)}{message}");

static async Task Show(string label, Action<IApplicationBuilder> configure)
{
    Console.WriteLine($"  {label}:");
    Depth = 1;

    var services = new ServiceCollection();
    using var provider = services.BuildServiceProvider();
    var app = new ApplicationBuilder(provider);
    configure(app);

    var context = new DefaultHttpContext { RequestServices = provider };
    context.Response.Body = Stream.Null;
    await app.Build()(context);
}

internal static partial class Program
{
    private static int Depth { get; set; } = 1;
}
