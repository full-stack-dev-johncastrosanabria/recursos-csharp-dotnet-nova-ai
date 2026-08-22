// The three lifetimes, shown as instance identity rather than described.
//
// Each cell is how many distinct instances appeared. Read the rows against each
// other: the difference between them is the whole of the lifetime decision.

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<SingletonThing>();
services.AddScoped<ScopedThing>();
services.AddTransient<TransientThing>();

using var provider = services.BuildServiceProvider();

Console.WriteLine("Distinct instances observed:");
Console.WriteLine();
Console.WriteLine($"  {"lifetime",-12}{"twice in one scope",22}{"once in each of 3 scopes",26}");
Console.WriteLine("  " + new string('-', 60));

Report<SingletonThing>("singleton");
Report<ScopedThing>("scoped");
Report<TransientThing>("transient");

Console.WriteLine();
Console.WriteLine("A singleton is one per container. A scoped service is one per scope -- in a");
Console.WriteLine("web application the scope is the request, so 'scoped' and 'per request' mean");
Console.WriteLine("the same thing only because something creates a scope per request. A");
Console.WriteLine("transient is a new instance at every injection point, which is the row");
Console.WriteLine("people underestimate: two collaborators that each take one do not share.");
Console.WriteLine();
Console.WriteLine("None of that is visible where the service is used. The class takes an");
Console.WriteLine("interface; a registration line in another file decides whether its state is");
Console.WriteLine("shared with the rest of the request, the rest of the process, or nobody.");
Console.WriteLine("That is why lifetime mistakes read as 'impossible' bugs: the code at the");
Console.WriteLine("crime scene is correct.");
Console.WriteLine();
Console.WriteLine("Rules of thumb worth having. Scoped for anything touching a unit of work.");
Console.WriteLine("Singleton for stateless things that are expensive to build. Transient for");
Console.WriteLine("small stateless things -- and never as the 'safe' default, because it");
Console.WriteLine("multiplies quietly and, if the type is disposable, accumulates.");

void Report<T>(string label)
    where T : notnull
{
    using var single = provider.CreateScope();
    var twice = new HashSet<Guid>
    {
        Id(single.ServiceProvider.GetRequiredService<T>()),
        Id(single.ServiceProvider.GetRequiredService<T>()),
    };

    var across = new HashSet<Guid>();
    for (var i = 0; i < 3; i++)
    {
        using var scope = provider.CreateScope();
        across.Add(Id(scope.ServiceProvider.GetRequiredService<T>()));
    }

    Console.WriteLine($"  {label,-12}{twice.Count,22}{across.Count,26}");
}

static Guid Id(object instance) => ((IIdentifiable)instance).InstanceId;

internal interface IIdentifiable
{
    Guid InstanceId { get; }
}

internal sealed class SingletonThing : IIdentifiable
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

internal sealed class ScopedThing : IIdentifiable
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

internal sealed class TransientThing : IIdentifiable
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}
