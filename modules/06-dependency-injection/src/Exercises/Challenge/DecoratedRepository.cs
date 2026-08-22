using Microsoft.Extensions.DependencyInjection;

namespace Training.Module06.Challenge;

public sealed class CallLog
{
    public IList<string> Calls { get; } = [];
}

public interface IOrderRepository
{
    string Find(string orderId);
}

public sealed class SqlOrderRepository(CallLog log) : IOrderRepository
{
    public string Find(string orderId)
    {
        log.Calls.Add($"db:{orderId}");
        return $"order {orderId}";
    }
}

/// <summary>
/// Wraps another repository and remembers what it returned.
///
/// Challenge: log "cache:hit {id}" and return the remembered value when there
/// is one; otherwise log "cache:miss {id}", ask the inner repository, remember
/// the answer and return it.
/// </summary>
public sealed class CachingOrderRepository : IOrderRepository
{
    public CachingOrderRepository(IOrderRepository inner, CallLog log)
    {
    }

    public string Find(string orderId) => throw new NotImplementedException();
}

public sealed class OrderLookup(IOrderRepository repository)
{
    public string Describe(string orderId) => repository.Find(orderId);
}

/// <summary>
/// Exercise: register these so that resolving IOrderRepository gives the
/// caching decorator wrapping the SQL one, while SqlOrderRepository stays
/// resolvable on its own.
///
/// The trap is registering the concrete type and the interface separately: that
/// produces two objects, so the cache the decorator holds is not the one
/// anybody is using, and the decoration silently does nothing.
/// </summary>
public static class DecoratedRepository
{
    public static IServiceCollection Register(IServiceCollection services, CallLog log)
        => throw new NotImplementedException();
}
