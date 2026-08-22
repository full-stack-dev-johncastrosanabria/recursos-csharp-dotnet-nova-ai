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
/// Challenge: resolve every handler registered for the message's type, hand the
/// message to each, and return how many ran. A message with no handlers is not
/// an error, which is the hazard worth knowing: the dispatcher reports success
/// having done nothing at all.
/// </summary>
public sealed class MessageDispatcher
{
    public MessageDispatcher(IServiceProvider provider)
    {
    }

    public int Dispatch<T>(T message) => throw new NotImplementedException();
}

/// <summary>
/// Exercise: register DefaultValidator as an open generic so every closed type
/// is served by one line, register the specific payment validator so it wins
/// for its own type, register both OrderPlaced handlers, and register the
/// dispatcher.
/// </summary>
public static class OpenGenericHandlers
{
    public static IServiceCollection Register(IServiceCollection services)
        => throw new NotImplementedException();
}
