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
/// The three lifetimes, and the one sentence that distinguishes them: a
/// singleton is one per container, a scoped service is one per scope, and a
/// transient is one per injection point.
///
/// The pairs make the practical difference visible. Two collaborators that each
/// take the same dependency share one instance if it is scoped and get separate
/// instances if it is transient. Nothing about the code says which, so the
/// moment that dependency carries any state, the registration line -- in a
/// different file -- decides whether the state is shared.
///
/// The default worth reaching for is scoped for anything touching a request,
/// singleton for anything stateless and expensive to build, and transient for
/// small stateless things. Transient is not the "safe" choice: it is the one
/// most likely to surprise, because it silently multiplies.
/// </summary>
public static class ServiceLifetimes
{
    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddSingleton<SingletonCounter>();
        services.AddScoped<ScopedCounter>();
        services.AddTransient<TransientCounter>();
        services.AddTransient<TransientPair>();
        services.AddTransient<ScopedPair>();

        return services;
    }
}
