using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Training.Module05.Core;

public sealed class FeatureOptions
{
    public const string SectionName = "Features";

    public bool CheckoutV2Enabled { get; set; }
}

/// <summary>
/// Captures a flag once, at construction.
///
/// This is the module's real-world case, and nothing about it looks wrong.
/// IOptions&lt;T&gt; is a singleton whose Value is computed once and cached, so a
/// singleton that reads it in its constructor has taken a photograph. Every
/// later configuration change is invisible to this object for the life of the
/// process -- and it is a *correct* use of IOptions if the value genuinely
/// cannot change, which is why review does not catch it.
/// </summary>
public sealed class StartupFlagCache
{
    public StartupFlagCache(IOptions<FeatureOptions> options)
        => CheckoutV2Enabled = options.Value.CheckoutV2Enabled;

    public bool CheckoutV2Enabled { get; }
}

/// <summary>
/// Reads the same flag three ways so the difference is visible.
/// </summary>
public sealed class FeatureFlagReader(
    StartupFlagCache startup,
    IOptionsSnapshot<FeatureOptions> snapshot,
    IOptionsMonitor<FeatureOptions> monitor)
{
    /// <summary>What the singleton captured at startup. Never changes.</summary>
    public bool CapturedAtStartup => startup.CheckoutV2Enabled;

    /// <summary>Fixed for the life of this scope. Recomputed in the next one.</summary>
    public bool PerScope => snapshot.Value.CheckoutV2Enabled;

    /// <summary>Follows configuration immediately.</summary>
    public bool Live => monitor.CurrentValue.CheckoutV2Enabled;
}

/// <summary>
/// Choosing between the three is the actual skill.
///
/// IOptions for values that cannot change after startup -- a connection string,
/// a listening port. IOptionsSnapshot for values that may change but must stay
/// consistent within one request; a flag flipping mid-request would mean a
/// single operation taking both branches. IOptionsMonitor for a singleton, or
/// anywhere you genuinely want the latest value now.
///
/// Note the constraint the container enforces: IOptionsSnapshot is scoped, so a
/// singleton cannot take one. That refusal is the container telling you the
/// answer -- a singleton wanting live values wants the monitor.
/// </summary>
public static class OptionsLifetimes
{
    public static IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeatureOptions>(configuration.GetSection(FeatureOptions.SectionName));
        services.AddSingleton<StartupFlagCache>();
        services.AddScoped<FeatureFlagReader>();

        return services;
    }
}
