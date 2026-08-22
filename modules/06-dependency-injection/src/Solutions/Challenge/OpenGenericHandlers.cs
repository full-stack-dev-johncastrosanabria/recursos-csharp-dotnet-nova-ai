using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Challenge;

public sealed record OrderPlaced(string OrderId);

public sealed record ShipmentBooked(string ShipmentId);

public sealed record PaymentTaken(string PaymentId);

public interface IValidator<T>
{
    string Name { get; }
}

public sealed class DefaultValidator<T> : IValidator<T>
{
    public string Name => "default";
}

public sealed class PaymentValidator : IValidator<PaymentTaken>
{
    public string Name => "payment-specific";
}

public interface IHandler<T>
{
    string Name { get; }

    void Handle(T message);
}

public sealed class AuditHandler : IHandler<OrderPlaced>
{
    public string Name => "audit";

    public IList<string> Handled { get; } = [];

    public void Handle(OrderPlaced message) => Handled.Add(message.OrderId);
}

public sealed class EmailHandler : IHandler<OrderPlaced>
{
    public string Name => "email";

    public IList<string> Handled { get; } = [];

    public void Handle(OrderPlaced message) => Handled.Add(message.OrderId);
}

/// <summary>
/// Runs every handler registered for a message.
///
/// This one takes IServiceProvider deliberately, which is the exception to the
/// rule stated in PaymentRouter. A dispatcher's whole job is to resolve
/// something whose type is not known until the call, so there is nothing to put
/// in a constructor. That is the narrow case where service location is the
/// right answer -- and it stays narrow: this class resolves exactly one open
/// generic and does nothing else with the provider.
///
/// Note what an empty handler set does. GetServices never throws and never
/// returns null, so a message nobody handles dispatches successfully to nobody.
/// Returning the count is the cheapest way to make that visible; a real
/// dispatcher would usually log or fail on zero for message types that must be
/// handled.
/// </summary>
public sealed class MessageDispatcher(IServiceProvider provider)
{
    public int Dispatch<T>(T message)
    {
        var ran = 0;

        foreach (var handler in provider.GetServices<IHandler<T>>())
        {
            handler.Handle(message);
            ran++;
        }

        return ran;
    }
}

/// <summary>
/// Open generic registration: one line that serves every closed type.
///
/// `typeof(IValidator&lt;&gt;)` to `typeof(DefaultValidator&lt;&gt;)` covers
/// IValidator&lt;OrderPlaced&gt;, IValidator&lt;ShipmentBooked&gt; and every type
/// added later. The alternative -- a registration per closed type -- is a new
/// line every time the domain grows, and a silent gap the first time somebody
/// forgets one.
///
/// A specific registration for a closed type still wins, because it is
/// registered after the open generic and a single resolve returns the last
/// match. That is the same last-wins rule as RegistrationRules, and it is what
/// lets one type opt out of a general policy.
/// </summary>
public static class OpenGenericHandlers
{
    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddTransient(typeof(IValidator<>), typeof(DefaultValidator<>));
        services.AddTransient<IValidator<PaymentTaken>, PaymentValidator>();

        services.AddTransient<IHandler<OrderPlaced>, AuditHandler>();
        services.AddTransient<IHandler<OrderPlaced>, EmailHandler>();

        services.AddSingleton<MessageDispatcher>();

        return services;
    }
}
