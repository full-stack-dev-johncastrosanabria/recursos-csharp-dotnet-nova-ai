using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

public sealed class SingletonCounter
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

public sealed class ScopedCounter
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

public sealed class TransientCounter
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

public sealed class TransientPair(TransientCounter first, TransientCounter second)
{
    public TransientCounter First { get; } = first;

    public TransientCounter Second { get; } = second;
}

public sealed class ScopedPair(ScopedCounter first, ScopedCounter second)
{
    public ScopedCounter First { get; } = first;

    public ScopedCounter Second { get; } = second;
}

/// <summary>
/// Exercise: register the five types above so each behaves as its name says.
///
/// The pairs are the interesting ones. Two collaborators that each take the
/// same dependency share it if it is scoped and do not if it is transient, and
/// that difference is invisible until something carries state.
/// </summary>
public static class ServiceLifetimes
{
    public static IServiceCollection Register(IServiceCollection services)
        => throw new NotImplementedException();
}
