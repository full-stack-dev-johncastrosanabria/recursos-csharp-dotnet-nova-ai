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
/// Challenge: declare the key in the constructor rather than reaching into the
/// provider to look one up. A service that takes IServiceProvider and resolves
/// from it has hidden its dependencies from everything that reads its
/// signature, including the container's own validation.
/// </summary>
public sealed class PaymentRouter
{
    public PaymentRouter(IPaymentGateway preferred) => throw new NotImplementedException();

    public string PreferredName => throw new NotImplementedException();
}

/// <summary>
/// Exercise: register both gateways under the keys "primary" and "fallback" as
/// singletons, and register the router.
///
/// Note how this differs from module 05's named options, which look similar: an
/// unknown key here is an error, where an unknown options name silently returns
/// defaults.
/// </summary>
public static class KeyedGateways
{
    public static IServiceCollection Register(IServiceCollection services)
        => throw new NotImplementedException();
}
