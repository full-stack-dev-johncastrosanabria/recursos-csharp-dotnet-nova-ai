using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

/// <summary>Stands in for a DbContext: scoped, stateful, not safe to share.</summary>
public sealed class OrderSession
{
    public Guid SessionId { get; } = Guid.NewGuid();
}

/// <summary>
/// The bug, and it is three tokens long: a singleton taking a scoped
/// dependency.
///
/// The container resolves OrderSession once, from the root scope, and this
/// object holds it for the life of the process. Every request afterwards shares
/// it. For a DbContext that means one connection and one change tracker used
/// concurrently, which surfaces as "a second operation was started on this
/// context" -- under load, days later, from a stack trace that names whichever
/// request happened to collide.
///
/// Nothing in this file is wrong on its own. The mistake is the pairing of two
/// lifetimes, and it lives in the registration, not here.
/// </summary>
public sealed class ReportSchedulerBroken(OrderSession session)
{
    public Guid SessionId => session.SessionId;
}

/// <summary>
/// The repair. A singleton cannot hold a scoped service, so it holds the
/// factory and opens a scope per unit of work.
///
/// The shape matters as much as the fix. A scope is a unit of work -- a
/// request, a message, a scheduled run -- and everything inside it shares one
/// session. Resolving a fresh session per call instead would mean a single
/// logical operation spanning two connections, which breaks transactions in a
/// way that is much harder to see than the bug it replaced.
/// </summary>
public sealed class ReportScheduler(IServiceScopeFactory scopeFactory)
{
    public Guid RunOnce()
    {
        using var scope = scopeFactory.CreateScope();

        return scope.ServiceProvider.GetRequiredService<OrderSession>().SessionId;
    }

    public bool RunOnceObservingTwice()
    {
        using var scope = scopeFactory.CreateScope();

        var first = scope.ServiceProvider.GetRequiredService<OrderSession>();
        var second = scope.ServiceProvider.GetRequiredService<OrderSession>();

        return first.SessionId == second.SessionId;
    }
}

/// <summary>
/// The registrations, and the flag that would have caught the bad one.
///
/// BuildServiceProvider validates nothing by default, which is why the captive
/// dependency builds happily. ValidateScopes turns it into a startup failure
/// naming both services; ValidateOnBuild additionally tries to construct every
/// registration, so a missing dependency also fails at boot rather than on the
/// request that first needed it.
///
/// The default host enables both in the Development environment only. That is a
/// reasonable default and a trap worth knowing: the check that would catch this
/// is off in exactly the environment where it matters.
/// </summary>
public static class CaptiveDependency
{
    public static IServiceCollection RegisterBroken(IServiceCollection services)
    {
        services.AddScoped<OrderSession>();
        services.AddSingleton<ReportSchedulerBroken>();

        return services;
    }

    public static IServiceCollection RegisterFixed(IServiceCollection services)
    {
        services.AddScoped<OrderSession>();
        services.AddSingleton<ReportScheduler>();

        return services;
    }
}
