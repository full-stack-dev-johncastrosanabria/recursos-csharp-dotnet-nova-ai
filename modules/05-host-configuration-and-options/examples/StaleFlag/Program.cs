// The module's real-world case. A feature flag is changed in configuration and
// the running service keeps the old value -- not everywhere, only in the places
// that captured IOptions<T>.
//
// The flag is flipped once, in the middle. Watch which columns move.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Features:CheckoutV2Enabled"] = "false",
    })
    .Build();

var services = new ServiceCollection();
services.Configure<FeatureOptions>(configuration.GetSection("Features"));
services.AddSingleton<CapturedAtStartup>();
services.AddScoped<PerRequest>();
services.AddSingleton<AlwaysLive>();

using var provider = services.BuildServiceProvider();

// Host startup: the singleton is constructed, so it reads the flag now.
var captured = provider.GetRequiredService<CapturedAtStartup>();
var live = provider.GetRequiredService<AlwaysLive>();

Console.WriteLine("One flag, read three ways, across a configuration change.");
Console.WriteLine();
Console.WriteLine($"  {"moment",-34}{"IOptions",11}{"Snapshot",11}{"Monitor",10}");
Console.WriteLine("  " + new string('-', 66));

Report("at startup");

configuration["Features:CheckoutV2Enabled"] = "true";
configuration.Reload();

Report("after the flag is flipped");
Report("a later request entirely");

Console.WriteLine();
Console.WriteLine("The IOptions column never moves. It is a singleton whose Value is computed");
Console.WriteLine("once and cached, so the service that captured it at startup is reading a");
Console.WriteLine("photograph for the rest of the process. Restarting 'fixes' it, which is how");
Console.WriteLine("this gets misfiled as a deployment quirk rather than a bug.");
Console.WriteLine();
Console.WriteLine("The Snapshot column moves, but only between requests -- it is fixed inside");
Console.WriteLine("a scope. That is usually what you want: a flag flipping halfway through a");
Console.WriteLine("request would mean one operation taking both branches.");
Console.WriteLine();
Console.WriteLine("The Monitor column follows immediately, which is what a singleton needs.");
Console.WriteLine();
Console.WriteLine("So the rule is not 'always use the monitor'. It is:");
Console.WriteLine();
Console.WriteLine("  IOptions          the value cannot change after startup");
Console.WriteLine("  IOptionsSnapshot  it can change, but must be stable within a request");
Console.WriteLine("  IOptionsMonitor   a singleton, or you genuinely want the latest value now");
Console.WriteLine();
Console.WriteLine("The container already enforces half of this: IOptionsSnapshot is scoped, so");
Console.WriteLine("a singleton cannot take one. When it refuses, it is answering the question.");
Console.WriteLine();
Console.WriteLine("Here the reload is explicit. In a real host it arrives on its own -- from");
Console.WriteLine("AddJsonFile(..., reloadOnChange: true), or a configuration server -- which");
Console.WriteLine("is why nobody is watching when the value changes.");

void Report(string moment)
{
    using var scope = provider.CreateScope();
    var perRequest = scope.ServiceProvider.GetRequiredService<PerRequest>();

    Console.WriteLine(
        $"  {moment,-34}{captured.Enabled,11}{perRequest.Enabled,11}{live.Enabled,10}");
}

internal sealed class FeatureOptions
{
    public bool CheckoutV2Enabled { get; set; }
}

/// <summary>Reads IOptions once, in its constructor. The bug, in three lines.</summary>
internal sealed class CapturedAtStartup(IOptions<FeatureOptions> options)
{
    public bool Enabled { get; } = options.Value.CheckoutV2Enabled;
}

internal sealed class PerRequest(IOptionsSnapshot<FeatureOptions> snapshot)
{
    public bool Enabled => snapshot.Value.CheckoutV2Enabled;
}

internal sealed class AlwaysLive(IOptionsMonitor<FeatureOptions> monitor)
{
    public bool Enabled => monitor.CurrentValue.CheckoutV2Enabled;
}
