// The same broken configuration, registered two ways. One host refuses to
// start. The other starts happily and fails later, somewhere else, at whatever
// moment a request first needs the value.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("Configuration is missing Payments:ApiKey in both cases.");
Console.WriteLine();

Console.WriteLine("1. Validated lazily (no ValidateOnStart)");
using (var host = BuildHost(validateOnStart: false))
{
    await host.StartAsync();
    Console.WriteLine("   host started        <- reports healthy, takes traffic");

    try
    {
        var options = host.Services.GetRequiredService<IOptions<PaymentOptions>>().Value;
        Console.WriteLine($"   used options        {options.ApiKey}");
    }
    catch (OptionsValidationException error)
    {
        Console.WriteLine($"   first use threw     {error.Message}");
    }

    await host.StopAsync();
}

Console.WriteLine();
Console.WriteLine("2. Validated at startup");
using (var host = BuildHost(validateOnStart: true))
{
    try
    {
        await host.StartAsync();
        Console.WriteLine("   host started");
    }
    catch (OptionsValidationException error)
    {
        Console.WriteLine($"   start refused       {error.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Both find the same problem. They differ in when, and in who is watching.");
Console.WriteLine();
Console.WriteLine("The lazy one fails on the first request that happens to need payments.");
Console.WriteLine("That may be minutes after the deploy or hours, it affects only the");
Console.WriteLine("instances that have taken such a request, and the stack trace describes a");
Console.WriteLine("checkout rather than a configuration mistake. A rolling deploy can put six");
Console.WriteLine("instances into that state and report every one of them healthy, because");
Console.WriteLine("nothing has asked for the value yet.");
Console.WriteLine();
Console.WriteLine("The eager one fails during startup, so the instance never becomes healthy,");
Console.WriteLine("the rollout halts on the first instance, and the message names the setting.");
Console.WriteLine();
Console.WriteLine("This is one method call. It is the highest-value line in this module.");

static IHost BuildHost(bool validateOnStart)
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:TimeoutSeconds"] = "30",
        })
        .Build();

    var builder = Host.CreateEmptyApplicationBuilder(null);

    var options = builder.Services
        .AddOptions<PaymentOptions>()
        .Bind(configuration.GetSection("Payments"))
        .ValidateDataAnnotations();

    if (validateOnStart)
    {
        options.ValidateOnStart();
    }

    return builder.Build();
}

internal sealed class PaymentOptions
{
    [System.ComponentModel.DataAnnotations.Required]
    public string ApiKey { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 30;
}
