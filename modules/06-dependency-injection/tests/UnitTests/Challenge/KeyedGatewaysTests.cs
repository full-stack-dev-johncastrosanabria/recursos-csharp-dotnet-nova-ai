using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Challenge;

namespace Training.Module06.Tests.Challenge;

public sealed class KeyedGatewaysTests
{
    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        KeyedGateways.Register(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Each_key_resolves_its_own_implementation()
    {
        using var provider = Build();

        provider.GetRequiredKeyedService<IPaymentGateway>("primary").Name.ShouldBe("primary");
        provider.GetRequiredKeyedService<IPaymentGateway>("fallback").Name.ShouldBe("fallback");
    }

    [Fact]
    public void An_unknown_key_throws_rather_than_returning_a_default()
    {
        // Unlike named options in module 05, a missing key here is an error.
        // Worth knowing, because the two systems look alike and behave
        // differently on exactly this point.
        using var provider = Build();

        Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredKeyedService<IPaymentGateway>("nope"));
    }

    [Fact]
    public void An_unknown_key_is_null_from_the_non_throwing_overload()
    {
        using var provider = Build();

        provider.GetKeyedService<IPaymentGateway>("nope").ShouldBeNull();
    }

    [Fact]
    public void A_keyed_registration_is_invisible_to_an_unkeyed_resolve()
    {
        using var provider = Build();

        provider.GetService<IPaymentGateway>().ShouldBeNull();
    }

    [Fact]
    public void A_consumer_can_declare_which_key_it_wants()
    {
        // The attribute is what keeps the key out of the consumer's body: the
        // router asks for "the primary gateway" in its signature rather than
        // reaching into the provider to look one up.
        using var provider = Build();

        provider.GetRequiredService<PaymentRouter>().PreferredName.ShouldBe("primary");
    }

    [Fact]
    public void Keyed_lifetimes_behave_like_their_unkeyed_equivalents()
    {
        using var provider = Build();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredKeyedService<IPaymentGateway>("primary");
        var b = second.ServiceProvider.GetRequiredKeyedService<IPaymentGateway>("primary");

        ReferenceEquals(a, b).ShouldBeTrue();
    }
}
