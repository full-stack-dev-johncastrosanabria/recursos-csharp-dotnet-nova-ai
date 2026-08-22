using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Challenge;

namespace Training.Module06.Tests.Challenge;

public sealed class OpenGenericHandlersTests
{
    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        OpenGenericHandlers.Register(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void An_open_generic_registration_serves_every_closed_type()
    {
        // One registration, any number of closed types. Registering
        // IValidator<OrderPlaced> and IValidator<ShipmentBooked> separately
        // means a new line every time the domain grows -- and a silent gap
        // whenever somebody forgets one.
        using var provider = Build();

        provider.GetRequiredService<IValidator<OrderPlaced>>().ShouldNotBeNull();
        provider.GetRequiredService<IValidator<ShipmentBooked>>().ShouldNotBeNull();
    }

    [Fact]
    public void The_closed_types_are_genuinely_different_services()
    {
        using var provider = Build();

        provider.GetRequiredService<IValidator<OrderPlaced>>()
            .ShouldNotBeSameAs(provider.GetRequiredService<IValidator<ShipmentBooked>>());
    }

    [Fact]
    public void A_specific_registration_is_preferred_over_the_open_generic()
    {
        using var provider = Build();

        provider.GetRequiredService<IValidator<PaymentTaken>>().Name.ShouldBe("payment-specific");
    }

    [Fact]
    public void Every_handler_for_a_message_is_resolvable_together()
    {
        using var provider = Build();

        provider.GetRequiredService<IEnumerable<IHandler<OrderPlaced>>>()
            .Select(h => h.Name)
            .Order()
            .ShouldBe(["audit", "email"]);
    }

    [Fact]
    public void A_message_with_no_handlers_resolves_to_an_empty_set()
    {
        // Not an error, which is the hazard: a dispatcher built on this reports
        // success having done nothing at all.
        using var provider = Build();

        provider.GetRequiredService<IEnumerable<IHandler<ShipmentBooked>>>().ShouldBeEmpty();
    }

    [Fact]
    public void The_dispatcher_reports_how_many_handlers_actually_ran()
    {
        using var provider = Build();
        var dispatcher = provider.GetRequiredService<MessageDispatcher>();

        dispatcher.Dispatch(new OrderPlaced("ord_1")).ShouldBe(2);
        dispatcher.Dispatch(new ShipmentBooked("shp_1")).ShouldBe(0);
    }
}
