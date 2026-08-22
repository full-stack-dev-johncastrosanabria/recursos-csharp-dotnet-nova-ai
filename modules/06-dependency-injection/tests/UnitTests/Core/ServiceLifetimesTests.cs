using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Core;

namespace Training.Module06.Tests.Core;

public sealed class ServiceLifetimesTests
{
    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        ServiceLifetimes.Register(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void A_singleton_is_one_instance_for_the_whole_container()
    {
        using var provider = Build();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<SingletonCounter>();
        var b = second.ServiceProvider.GetRequiredService<SingletonCounter>();

        a.InstanceId.ShouldBe(b.InstanceId);
    }

    [Fact]
    public void A_scoped_service_is_one_instance_per_scope()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        var a = scope.ServiceProvider.GetRequiredService<ScopedCounter>();
        var b = scope.ServiceProvider.GetRequiredService<ScopedCounter>();

        a.InstanceId.ShouldBe(b.InstanceId);
    }

    [Fact]
    public void A_scoped_service_differs_between_scopes()
    {
        // This is what "per request" means in a web application: the scope is
        // the request, so a scoped service is shared by everything handling it
        // and by nothing outside it.
        using var provider = Build();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<ScopedCounter>();
        var b = second.ServiceProvider.GetRequiredService<ScopedCounter>();

        a.InstanceId.ShouldNotBe(b.InstanceId);
    }

    [Fact]
    public void A_transient_service_is_a_new_instance_every_time()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        var a = scope.ServiceProvider.GetRequiredService<TransientCounter>();
        var b = scope.ServiceProvider.GetRequiredService<TransientCounter>();

        a.InstanceId.ShouldNotBe(b.InstanceId);
    }

    [Fact]
    public void Transient_means_new_even_within_one_resolution()
    {
        // Two collaborators that each take a transient do not share one. If the
        // transient carries state anybody expects to be shared, this is where
        // that expectation quietly stops holding.
        using var provider = Build();
        using var scope = provider.CreateScope();

        var pair = scope.ServiceProvider.GetRequiredService<TransientPair>();

        pair.First.InstanceId.ShouldNotBe(pair.Second.InstanceId);
    }

    [Fact]
    public void Scoped_means_shared_within_one_resolution()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        var pair = scope.ServiceProvider.GetRequiredService<ScopedPair>();

        pair.First.InstanceId.ShouldBe(pair.Second.InstanceId);
    }
}
