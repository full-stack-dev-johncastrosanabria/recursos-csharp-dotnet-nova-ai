using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Core;

namespace Training.Module06.Tests.Core;

public sealed class ContainerDisposalTests
{
    [Fact]
    public void A_scoped_disposable_is_disposed_when_its_scope_ends()
    {
        var log = new DisposalLog();
        var services = new ServiceCollection();
        ContainerDisposal.Register(services, log);
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<ScopedResource>();
            log.Disposed.ShouldBeEmpty();
        }

        log.Disposed.ShouldBe(["scoped"]);
    }

    [Fact]
    public void A_singleton_disposable_is_disposed_with_the_container()
    {
        var log = new DisposalLog();
        var services = new ServiceCollection();
        ContainerDisposal.Register(services, log);
        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<SingletonResource>();
        log.Disposed.ShouldBeEmpty();

        provider.Dispose();

        log.Disposed.ShouldContain("singleton");
    }

    [Fact]
    public void A_transient_disposable_resolved_in_a_scope_waits_for_that_scope()
    {
        // The container owns everything it creates. A transient is not
        // disposed when you stop using it -- it is disposed when the scope that
        // created it ends, however long that is.
        var log = new DisposalLog();
        var services = new ServiceCollection();
        ContainerDisposal.Register(services, log);
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<TransientResource>();
            _ = scope.ServiceProvider.GetRequiredService<TransientResource>();
            log.Disposed.ShouldBeEmpty();
        }

        log.Disposed.Count(d => d == "transient").ShouldBe(2);
    }

    [Fact]
    public void A_transient_disposable_resolved_from_the_root_lives_for_the_process()
    {
        // The leak. Resolving a transient disposable from the root provider
        // registers it for disposal at container shutdown, so a long-lived
        // singleton that resolves one per operation accumulates them forever --
        // reachable, by design, and therefore invisible to a leak detector.
        var log = new DisposalLog();
        var services = new ServiceCollection();
        ContainerDisposal.Register(services, log);
        var provider = services.BuildServiceProvider();

        for (var i = 0; i < 50; i++)
        {
            _ = provider.GetRequiredService<TransientResource>();
        }

        log.Disposed.ShouldBeEmpty();

        provider.Dispose();

        log.Disposed.Count(d => d == "transient").ShouldBe(50);
    }

    [Fact]
    public void An_instance_you_registered_yourself_is_not_the_containers_to_dispose()
    {
        // Registering an existing instance hands over a reference, not
        // ownership -- so whoever created it still has to dispose it.
        var log = new DisposalLog();
        var mine = new ExternalResource(log);
        var services = new ServiceCollection();
        ContainerDisposal.RegisterExternalInstance(services, mine);
        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<ExternalResource>();
        provider.Dispose();

        log.Disposed.ShouldNotContain("external");
    }

    [Fact]
    public void Disposal_runs_in_reverse_order_of_creation()
    {
        var log = new DisposalLog();
        var services = new ServiceCollection();
        ContainerDisposal.Register(services, log);
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<ScopedResource>();
            _ = scope.ServiceProvider.GetRequiredService<TransientResource>();
        }

        log.Disposed.ShouldBe(["transient", "scoped"]);
    }
}
