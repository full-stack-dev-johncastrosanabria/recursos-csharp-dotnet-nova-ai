using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Core;

/// <summary>Stands in for a DbContext: scoped, stateful, not safe to share.</summary>
public sealed class OrderSession
{
    public Guid SessionId { get; } = Guid.NewGuid();
}

/// <summary>
/// The bug. A singleton that takes a scoped dependency in its constructor.
///
/// The container resolves it once, from the root scope, and the singleton holds
/// it for the life of the process. For a DbContext that means one connection
/// and one change tracker shared by every request forever -- which surfaces as
/// "a second operation was started on this context" under concurrency, days
/// later, nowhere near this line.
/// </summary>
public sealed class ReportSchedulerBroken(OrderSession session)
{
    public Guid SessionId => session.SessionId;
}

/// <summary>
/// The repair. A singleton cannot hold a scoped service, so it holds the
/// factory and opens a scope per unit of work.
///
/// Exercise: RunOnce creates a scope, resolves one session inside it, and
/// returns that session's id. RunOnceObservingTwice creates one scope, resolves
/// twice within it, and reports whether both resolutions saw the same session --
/// which they must, or a single unit of work spans two connections.
/// </summary>
public sealed class ReportScheduler
{
    // An explicit constructor rather than a primary one: a primary constructor
    // parameter that nothing reads is CS9113, and this repo relaxes exactly
    // four analyser rules in Exercises, none of which is that one. Your
    // implementation is free to use either form.
    public ReportScheduler(IServiceScopeFactory scopeFactory)
    {
    }

    public Guid RunOnce() => throw new NotImplementedException();

    public bool RunOnceObservingTwice() => throw new NotImplementedException();
}

/// <summary>
/// Exercise: RegisterBroken wires the captive version; RegisterFixed wires the
/// repaired one. Both register OrderSession as scoped.
/// </summary>
public static class CaptiveDependency
{
    public static IServiceCollection RegisterBroken(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterFixed(IServiceCollection services)
        => throw new NotImplementedException();
}
