using Microsoft.Extensions.Configuration;
using Shouldly;
using Training.Module05.Challenge;

namespace Training.Module05.Tests.Challenge;

public sealed class ConfigurationWatcherTests
{
    private static IConfigurationRoot Configuration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Flag"] = "off" })
            .Build();

    [Fact]
    public void The_current_value_is_available_immediately()
    {
        var configuration = Configuration();
        using var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        watcher.Current.ShouldBe("off");
    }

    [Fact]
    public void A_change_updates_the_current_value()
    {
        var configuration = Configuration();
        using var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        configuration["Features:Flag"] = "on";
        configuration.Reload();

        watcher.Current.ShouldBe("on");
    }

    [Fact]
    public void Every_change_is_counted()
    {
        var configuration = Configuration();
        using var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        configuration["Features:Flag"] = "on";
        configuration.Reload();
        configuration["Features:Flag"] = "off";
        configuration.Reload();

        watcher.Changes.ShouldBe(2);
    }

    [Fact]
    public void Re_registering_after_each_change_is_the_whole_trick()
    {
        // A change token fires once and is then spent. Register with
        // ChangeToken.OnChange, or re-register inside your own callback --
        // subscribe once by hand and you are notified of exactly one change and
        // then never again, which looks like the feature working.
        var configuration = Configuration();
        using var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        for (var i = 0; i < 5; i++)
        {
            configuration["Features:Flag"] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            configuration.Reload();
        }

        watcher.Changes.ShouldBe(5);
    }

    [Fact]
    public void Disposing_stops_the_notifications()
    {
        // The subscription roots the callback, which roots the watcher, which
        // roots whatever it holds -- module 02's event-handler leak, wearing
        // configuration's clothes.
        var configuration = Configuration();
        var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        configuration["Features:Flag"] = "on";
        configuration.Reload();
        watcher.Dispose();

        configuration["Features:Flag"] = "later";
        configuration.Reload();

        watcher.Changes.ShouldBe(1);
    }

    [Fact]
    public void Disposing_twice_is_harmless()
    {
        var configuration = Configuration();
        var watcher = new ConfigurationWatcher(configuration, "Features:Flag");

        watcher.Dispose();
        Should.NotThrow(watcher.Dispose);
    }
}
