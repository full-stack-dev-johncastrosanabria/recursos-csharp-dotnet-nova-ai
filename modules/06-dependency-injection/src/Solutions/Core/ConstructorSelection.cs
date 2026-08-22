using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

public sealed class AuditLog;

public sealed class MetricsSink;

/// <summary>
/// Two constructors, and the container picks the greediest one it can satisfy.
///
/// That rule is worth stating plainly because it makes registration a form of
/// action at a distance: adding MetricsSink to the container changes which
/// constructor of this class runs, and this file does not change. If two
/// constructors are equally satisfiable and neither is greedier, the container
/// refuses rather than guessing.
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
/// The container resolves services, not settings.
///
/// A string or an int has no registration to find, so a type carrying
/// configuration is either built by a factory delegate or -- better, and the
/// subject of module 05 -- takes IOptions and lets the options system do the
/// binding and the validation.
///
/// ValidateOnBuild is the other half of this. Without it, a missing dependency
/// is discovered on the first resolution that needs it, which in a web
/// application is a request. With it, the same mistake stops the process at
/// boot. Same trade as options validation, same one line.
/// </summary>
public static class ConstructorSelection
{
    public static IServiceCollection RegisterBothDependencies(IServiceCollection services)
    {
        services.AddSingleton<AuditLog>();
        services.AddSingleton<MetricsSink>();
        services.AddTransient<Dispatcher>();

        return services;
    }

    public static IServiceCollection RegisterAuditOnly(IServiceCollection services)
    {
        services.AddSingleton<AuditLog>();
        services.AddTransient<Dispatcher>();

        return services;
    }

    public static IServiceCollection RegisterNothing(IServiceCollection services)
    {
        services.AddTransient<Dispatcher>();

        return services;
    }

    public static IServiceCollection RegisterWithFactory(IServiceCollection services, string endpoint)
    {
        services.AddSingleton(_ => new EndpointClient(endpoint));

        return services;
    }
}
