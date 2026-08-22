using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Challenge;

namespace Training.Module06.Tests.Challenge;

public sealed class DecoratedRepositoryTests
{
    private static ServiceProvider Build(CallLog log)
    {
        var services = new ServiceCollection();
        DecoratedRepository.Register(services, log);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void The_decorated_service_still_answers_correctly()
    {
        var log = new CallLog();
        using var provider = Build(log);

        provider.GetRequiredService<IOrderRepository>().Find("ord_1").ShouldBe("order ord_1");
    }

    [Fact]
    public void The_decorator_runs_around_the_inner_implementation()
    {
        var log = new CallLog();
        using var provider = Build(log);

        provider.GetRequiredService<IOrderRepository>().Find("ord_1");

        log.Calls.ShouldBe(["cache:miss ord_1", "db:ord_1"]);
    }

    [Fact]
    public void A_second_call_is_served_without_reaching_the_inner_implementation()
    {
        var log = new CallLog();
        using var provider = Build(log);
        var repository = provider.GetRequiredService<IOrderRepository>();

        repository.Find("ord_1");
        log.Calls.Clear();
        repository.Find("ord_1");

        log.Calls.ShouldBe(["cache:hit ord_1"]);
    }

    [Fact]
    public void The_consumer_never_learns_it_was_decorated()
    {
        // The point of decorating through the container: callers depend on the
        // interface, and caching is added or removed by changing registration
        // rather than by touching a single call site.
        var log = new CallLog();
        using var provider = Build(log);

        provider.GetRequiredService<OrderLookup>().Describe("ord_2").ShouldBe("order ord_2");
    }

    [Fact]
    public void The_inner_implementation_is_still_resolvable_on_its_own()
    {
        var log = new CallLog();
        using var provider = Build(log);

        provider.GetRequiredService<SqlOrderRepository>().Find("ord_3").ShouldBe("order ord_3");
    }

    [Fact]
    public void Resolving_the_interface_does_not_give_the_undecorated_one()
    {
        // The trap when hand-rolling decoration: register the concrete type and
        // the interface separately and you get two objects, so the cache the
        // decorator holds is not the one anybody is using.
        var log = new CallLog();
        using var provider = Build(log);

        provider.GetRequiredService<IOrderRepository>()
            .ShouldNotBeOfType<SqlOrderRepository>();
    }
}
