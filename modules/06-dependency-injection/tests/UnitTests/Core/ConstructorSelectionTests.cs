using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Core;

namespace Training.Module06.Tests.Core;

public sealed class ConstructorSelectionTests
{
    [Fact]
    public void The_container_uses_the_greediest_constructor_it_can_satisfy()
    {
        // Not the first one, and not the simplest: the one with the most
        // parameters the container can supply. Registering one extra service
        // can therefore change which constructor runs, without touching the
        // class itself.
        var services = new ServiceCollection();
        ConstructorSelection.RegisterBothDependencies(services);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Dispatcher>().Description.ShouldBe("audit+metrics");
    }

    [Fact]
    public void Removing_a_registration_silently_selects_a_smaller_constructor()
    {
        var services = new ServiceCollection();
        ConstructorSelection.RegisterAuditOnly(services);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Dispatcher>().Description.ShouldBe("audit");
    }

    [Fact]
    public void A_dependency_nothing_can_supply_fails_at_resolution()
    {
        var services = new ServiceCollection();
        ConstructorSelection.RegisterNothing(services);
        using var provider = services.BuildServiceProvider();

        Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<Dispatcher>());
    }

    [Fact]
    public void With_several_constructors_the_failure_names_the_type_not_the_dependency()
    {
        // Worth knowing before you read one of these at 3am. With a single
        // constructor the container names the exact service it could not
        // resolve. With more than one it cannot say which you meant, so it
        // reports only that no constructor could be satisfied -- and diffing
        // the parameter lists against your registrations is left to you.
        //
        // That is a real argument for one constructor per type.
        var services = new ServiceCollection();
        ConstructorSelection.RegisterNothing(services);
        using var provider = services.BuildServiceProvider();

        var error = Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredService<Dispatcher>());

        error.Message.ShouldContain("No constructor");
        error.Message.ShouldContain("Dispatcher");
    }

    [Fact]
    public void ValidateOnBuild_moves_that_failure_to_startup()
    {
        // Same mistake, found at boot instead of on the request that first
        // needed it. This is the same trade as options validation in module 05,
        // and it is the same one line of configuration.
        var services = new ServiceCollection();
        ConstructorSelection.RegisterNothing(services);

        Should.Throw<AggregateException>(() => services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }));
    }

    [Fact]
    public void A_value_the_container_cannot_know_must_be_supplied_by_a_factory()
    {
        // The container resolves services, not settings. A string or an int has
        // no registration to find, so anything carrying configuration is built
        // by a factory delegate -- or, better, reads options.
        var services = new ServiceCollection();
        ConstructorSelection.RegisterWithFactory(services, "https://dispatch.internal");
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<EndpointClient>().Endpoint.ShouldBe("https://dispatch.internal");
    }
}
