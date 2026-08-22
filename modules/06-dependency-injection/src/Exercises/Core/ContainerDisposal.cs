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
/// Exercise: register the log and the three resources with the lifetimes their
/// names imply, and register the external instance as an instance.
///
/// The rule the container follows is: it disposes what it created, when the
/// scope that created it ends. Two consequences are worth meeting deliberately.
/// A transient disposable resolved from the root provider is held until the
/// container shuts down, so a singleton resolving one per operation accumulates
/// them for the life of the process. And an instance you constructed yourself
/// and handed over is not the container's to dispose.
/// </summary>
public static class ContainerDisposal
{
    public static IServiceCollection Register(IServiceCollection services, DisposalLog log)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterExternalInstance(
        IServiceCollection services,
        ExternalResource resource)
        => throw new NotImplementedException();
}
