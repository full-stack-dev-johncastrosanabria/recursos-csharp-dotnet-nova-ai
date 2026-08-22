using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

public sealed class AuditLog;

public sealed class MetricsSink;

/// <summary>
/// Two constructors. The container picks the one with the most parameters it
/// can satisfy -- so registering one extra service changes which constructor
/// runs, without this file changing at all.
/// </summary>
public sealed class Dispatcher
{
    public Dispatcher(AuditLog audit) => Description = "audit";

    public Dispatcher(AuditLog audit, MetricsSink metrics) => Description = "audit+metrics";

    public string Description { get; }
}

/// <summary>
/// Carries a value the container cannot possibly resolve, so it has to be
/// built by a factory.
/// </summary>
public sealed class EndpointClient(string endpoint)
{
    public string Endpoint { get; } = endpoint;
}

/// <summary>
/// Exercise: four registration sets that make the selection rules visible.
/// RegisterNothing registers the Dispatcher and neither dependency.
/// </summary>
public static class ConstructorSelection
{
    public static IServiceCollection RegisterBothDependencies(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterAuditOnly(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterNothing(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterWithFactory(IServiceCollection services, string endpoint)
        => throw new NotImplementedException();
}
