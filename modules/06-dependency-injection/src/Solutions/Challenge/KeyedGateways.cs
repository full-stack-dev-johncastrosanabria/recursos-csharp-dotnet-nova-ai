using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Challenge;

public interface IPaymentGateway
{
    string Name { get; }
}

public sealed class PrimaryGateway : IPaymentGateway
{
    public string Name => "primary";
}

public sealed class FallbackGateway : IPaymentGateway
{
    public string Name => "fallback";
}

/// <summary>
/// Asks for one particular gateway by key.
///
/// The attribute is what keeps the key in the signature. The alternative --
/// taking IServiceProvider and looking the gateway up inside the body -- is
/// service location, and it costs three things: the dependency no longer
/// appears in the constructor, so nothing reading the class can see it;
/// ValidateOnBuild cannot check it, because there is nothing to check; and the
/// class can now reach anything in the container, which is the opposite of what
/// injection is for.
/// </summary>
public sealed class PaymentRouter(
    [FromKeyedServices("primary")] IPaymentGateway preferred)
{
    public string PreferredName => preferred.Name;
}

/// <summary>
/// Keyed services: several implementations of one interface, told apart by key.
///
/// Compare with module 05's named options, which look like the same idea and
/// differ on exactly one point: asking for an unregistered key here is an
/// error, where asking for an unconfigured options name returns a defaulted
/// object. That difference is worth holding onto, because the two are often
/// used side by side -- a keyed client configured by named options -- and only
/// one of them will tell you about a typo.
/// </summary>
public static class KeyedGateways
{
    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddKeyedSingleton<IPaymentGateway, PrimaryGateway>("primary");
        services.AddKeyedSingleton<IPaymentGateway, FallbackGateway>("fallback");
        services.AddSingleton<PaymentRouter>();

        return services;
    }
}
