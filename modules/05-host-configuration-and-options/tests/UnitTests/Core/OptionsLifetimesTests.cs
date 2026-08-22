using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module05.Core;

namespace Training.Module05.Tests.Core;

public sealed class OptionsLifetimesTests
{
    private static (ServiceProvider Provider, IConfigurationRoot Configuration) Build()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CheckoutV2Enabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        OptionsLifetimes.Register(services, configuration);
        var provider = services.BuildServiceProvider();

        // Stand in for host startup. IOptions<T>.Value is computed on first
        // access, so the staleness only exists once something has actually
        // captured it -- which in a real service is a singleton built at boot.
        _ = provider.GetRequiredService<StartupFlagCache>();

        return (provider, configuration);
    }

    private static void ChangeFlag(IConfigurationRoot configuration, string value)
    {
        configuration["Features:CheckoutV2Enabled"] = value;
        configuration.Reload();
    }

    [Fact]
    public void All_three_agree_before_anything_changes()
    {
        var (provider, _) = Build();
        using var scope = provider.CreateScope();

        var reader = scope.ServiceProvider.GetRequiredService<FeatureFlagReader>();

        reader.CapturedAtStartup.ShouldBeFalse();
        reader.PerScope.ShouldBeFalse();
        reader.Live.ShouldBeFalse();

        provider.Dispose();
    }

    [Fact]
    public void The_captured_value_never_changes()
    {
        // This is the module's real-world case. IOptions<T> is a singleton
        // resolved once; the flag is flipped and this service never finds out,
        // for as long as the process lives.
        var (provider, configuration) = Build();
        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<FeatureFlagReader>();

        ChangeFlag(configuration, "true");

        reader.CapturedAtStartup.ShouldBeFalse();

        provider.Dispose();
    }

    [Fact]
    public void The_live_value_follows_configuration_immediately()
    {
        var (provider, configuration) = Build();
        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<FeatureFlagReader>();

        ChangeFlag(configuration, "true");

        reader.Live.ShouldBeTrue();

        provider.Dispose();
    }

    [Fact]
    public void The_per_scope_value_is_fixed_within_one_scope()
    {
        // A snapshot is stable for the lifetime of a request, which is usually
        // what you want: a flag flipping halfway through a request would mean
        // one operation taking both branches.
        var (provider, configuration) = Build();
        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<FeatureFlagReader>();

        _ = reader.PerScope;
        ChangeFlag(configuration, "true");

        reader.PerScope.ShouldBeFalse();

        provider.Dispose();
    }

    [Fact]
    public void A_new_scope_picks_up_the_new_value()
    {
        var (provider, configuration) = Build();

        using (var first = provider.CreateScope())
        {
            _ = first.ServiceProvider.GetRequiredService<FeatureFlagReader>().PerScope;
        }

        ChangeFlag(configuration, "true");

        using (var second = provider.CreateScope())
        {
            second.ServiceProvider.GetRequiredService<FeatureFlagReader>().PerScope.ShouldBeTrue();
        }

        provider.Dispose();
    }

    [Fact]
    public void The_captured_value_stays_stale_even_in_a_brand_new_scope()
    {
        // The failure is not scoped to one request. The singleton was built
        // once, so every scope for the rest of the process sees the old value.
        var (provider, configuration) = Build();

        ChangeFlag(configuration, "true");

        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<FeatureFlagReader>();

        reader.Live.ShouldBeTrue();
        reader.CapturedAtStartup.ShouldBeFalse();

        provider.Dispose();
    }
}
