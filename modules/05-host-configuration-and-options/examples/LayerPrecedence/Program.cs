// Configuration is layered, and the last layer to set a key wins. The useful
// part is that the runtime can tell you which one that was.

using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Payments:Endpoint"] = "https://payments.internal",
        ["Payments:TimeoutSeconds"] = "30",
        ["Payments:ApiKey"] = "from-appsettings",
    })
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Payments:TimeoutSeconds"] = "10",
        ["Payments:ApiKey"] = "from-environment",
    })
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Payments:ApiKey"] = "from-command-line",
    })
    .Build();

Console.WriteLine("Three layers, registered in this order:");
Console.WriteLine("  1. appsettings      2. environment      3. command line");
Console.WriteLine();
Console.WriteLine($"  {"key",-28}{"effective value",-28}{"set by layers"}");
Console.WriteLine("  " + new string('-', 74));

foreach (var key in (string[])["Payments:Endpoint", "Payments:TimeoutSeconds", "Payments:ApiKey"])
{
    var setters = configuration.Providers
        .Select((provider, index) => (Layer: index + 1, Found: provider.TryGet(key, out _)))
        .Where(entry => entry.Found)
        .Select(entry => entry.Layer.ToString(System.Globalization.CultureInfo.InvariantCulture));

    Console.WriteLine($"  {key,-28}{configuration[key],-28}{string.Join(", ", setters)}");
}

Console.WriteLine();
Console.WriteLine("Endpoint is set once and survives. TimeoutSeconds is set twice and the");
Console.WriteLine("later layer wins. ApiKey is set by all three, and only the last one counts.");
Console.WriteLine();
Console.WriteLine("Reverse the registration order and everything still 'works' -- in every");
Console.WriteLine("environment where the layers happen to agree. It stops working in the one");
Console.WriteLine("place they differ, which is production.");
Console.WriteLine();
Console.WriteLine("The runtime will also print the whole picture for you:");
Console.WriteLine();

foreach (var line in configuration.GetDebugView().Split('\n').Take(12))
{
    Console.WriteLine($"  {line.TrimEnd()}");
}

Console.WriteLine();
Console.WriteLine("GetDebugView names the winning provider for every key. It is the fastest");
Console.WriteLine("answer to 'where is this value coming from' -- and note that it prints");
Console.WriteLine("values in clear, so it belongs in a debugger, never in a log. Redacting a");
Console.WriteLine("dump before it is safe to emit is exercise 7.");
