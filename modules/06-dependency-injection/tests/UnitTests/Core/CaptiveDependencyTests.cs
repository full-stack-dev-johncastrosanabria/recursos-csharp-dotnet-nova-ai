using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Core;

namespace Training.Module06.Tests.Core;

public sealed class CaptiveDependencyTests
{
    [Fact]
    public void The_broken_registration_builds_when_nothing_is_validated()
    {
        // The default provider does not check. This is the whole problem: the
        // mistake is invisible at startup and stays invisible until concurrency
        // makes the shared instance misbehave, days later.
        var services = new ServiceCollection();
        CaptiveDependency.RegisterBroken(services);

        using var provider = services.BuildServiceProvider();

        Should.NotThrow(() => provider.GetRequiredService<ReportSchedulerBroken>());
    }

    [Fact]
    public void The_captured_scoped_service_is_the_same_instance_forever()
    {
        // A scoped service resolved into a singleton is resolved once, from the
        // root scope, and then held for the life of the process. Every request
        // afterwards shares it -- which for a DbContext means concurrent use of
        // one connection and one change tracker.
        var services = new ServiceCollection();
        CaptiveDependency.RegisterBroken(services);
        using var provider = services.BuildServiceProvider();

        var scheduler = provider.GetRequiredService<ReportSchedulerBroken>();
        var first = scheduler.SessionId;

        using (var scope = provider.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<OrderSession>();
        }

        scheduler.SessionId.ShouldBe(first);
    }

    [Fact]
    public void Scope_validation_refuses_to_build_the_broken_registration()
    {
        // One flag turns a silent runtime failure into a startup failure that
        // names both services. It costs a few milliseconds at boot.
        var services = new ServiceCollection();
        CaptiveDependency.RegisterBroken(services);

        var error = Should.Throw<AggregateException>(() =>
            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true,
            }));

        error.ToString().ShouldContain("OrderSession");
    }

    [Fact]
    public void The_repaired_registration_builds_under_validation()
    {
        var services = new ServiceCollection();
        CaptiveDependency.RegisterFixed(services);

        Should.NotThrow(() => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        }));
    }

    [Fact]
    public void The_repaired_scheduler_gets_a_fresh_session_per_unit_of_work()
    {
        var services = new ServiceCollection();
        CaptiveDependency.RegisterFixed(services);
        using var provider = services.BuildServiceProvider();

        var scheduler = provider.GetRequiredService<ReportScheduler>();

        var first = scheduler.RunOnce();
        var second = scheduler.RunOnce();

        first.ShouldNotBe(second);
    }

    [Fact]
    public void The_repaired_scheduler_shares_one_session_within_a_unit_of_work()
    {
        // Creating a scope per unit of work is the point -- not creating a new
        // session per call. Everything inside one run must see one session, or
        // a transaction spans two connections.
        var services = new ServiceCollection();
        CaptiveDependency.RegisterFixed(services);
        using var provider = services.BuildServiceProvider();

        var scheduler = provider.GetRequiredService<ReportScheduler>();

        scheduler.RunOnceObservingTwice().ShouldBeTrue();
    }
}
