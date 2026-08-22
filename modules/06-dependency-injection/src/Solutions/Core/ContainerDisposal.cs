using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

public sealed class DisposalLog
{
    public IList<string> Disposed { get; } = [];
}

public sealed class ScopedResource(DisposalLog log) : IDisposable
{
    public void Dispose() => log.Disposed.Add("scoped");
}

public sealed class SingletonResource(DisposalLog log) : IDisposable
{
    public void Dispose() => log.Disposed.Add("singleton");
}

public sealed class TransientResource(DisposalLog log) : IDisposable
{
    public void Dispose() => log.Disposed.Add("transient");
}

public sealed class ExternalResource(DisposalLog log) : IDisposable
{
    public void Dispose() => log.Disposed.Add("external");
}

/// <summary>
/// The container owns what it creates, and it releases it when the scope that
/// created it ends -- not when you stop using it.
///
/// Two consequences catch people.
///
/// A transient disposable resolved from the *root* provider is held until the
/// container shuts down. So a singleton that resolves one per operation
/// accumulates them for the life of the process: every instance is reachable
/// from the container's own disposal list, by design, which means a leak
/// detector reports nothing and a heap profiler shows a healthy list. It is
/// module 02's unbounded cache with the container holding the reference.
/// The fix is a scope per operation, exactly as in CaptiveDependency.
///
/// An instance you constructed and registered is not the container's to
/// dispose. Registering it hands over a reference, not ownership, so whoever
/// created it still has to release it.
/// </summary>
public static class ContainerDisposal
{
    public static IServiceCollection Register(IServiceCollection services, DisposalLog log)
    {
        services.AddSingleton(log);
        services.AddScoped<ScopedResource>();
        services.AddSingleton<SingletonResource>();
        services.AddTransient<TransientResource>();

        return services;
    }

    public static IServiceCollection RegisterExternalInstance(
        IServiceCollection services,
        ExternalResource resource)
    {
        services.AddSingleton(resource);

        return services;
    }
}
