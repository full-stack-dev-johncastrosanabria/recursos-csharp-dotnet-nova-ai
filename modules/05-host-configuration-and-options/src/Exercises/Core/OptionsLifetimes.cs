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
/// Captures a flag once, at construction. This is the module's real-world case
/// in miniature: a singleton that reads IOptions&lt;T&gt;.Value and keeps it.
/// </summary>
public sealed class StartupFlagCache
{
    public StartupFlagCache(IOptions<FeatureOptions> options)
        => throw new NotImplementedException();

    public bool CheckoutV2Enabled => throw new NotImplementedException();
}

/// <summary>
/// Reads the same flag three ways so the difference is visible.
/// </summary>
public sealed class FeatureFlagReader
{
    public FeatureFlagReader(
        StartupFlagCache startup,
        IOptionsSnapshot<FeatureOptions> snapshot,
        IOptionsMonitor<FeatureOptions> monitor)
        => throw new NotImplementedException();

    /// <summary>What the singleton captured at startup. Never changes.</summary>
    public bool CapturedAtStartup => throw new NotImplementedException();

    /// <summary>Fixed for the life of this scope. Recomputed in the next one.</summary>
    public bool PerScope => throw new NotImplementedException();

    /// <summary>Follows configuration immediately.</summary>
    public bool Live => throw new NotImplementedException();
}

/// <summary>
/// Exercise: register the options and the two services above so that each of
/// the three properties behaves as its summary describes. FeatureFlagReader is
/// per-scope; StartupFlagCache lives for the process.
/// </summary>
public static class OptionsLifetimes
{
    public static IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
        => throw new NotImplementedException();
}
