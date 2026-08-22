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
/// The decorator depends on the interface, not on the concrete repository, so
/// it can wrap anything that satisfies the contract -- including another
/// decorator. That is what makes this composable rather than a special case.
/// </summary>
public sealed class CachingOrderRepository(IOrderRepository inner, CallLog log) : IOrderRepository
{
    private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);

    public string Find(string orderId)
    {
        if (_cache.TryGetValue(orderId, out var cached))
        {
            log.Calls.Add($"cache:hit {orderId}");
            return cached;
        }

        log.Calls.Add($"cache:miss {orderId}");

        var found = inner.Find(orderId);
        _cache[orderId] = found;

        return found;
    }
}

public sealed class OrderLookup(IOrderRepository repository)
{
    public string Describe(string orderId) => repository.Find(orderId);
}

/// <summary>
/// Decoration through the container.
///
/// The factory registration is the whole technique: register the concrete type
/// normally, then register the interface as a delegate that builds the
/// decorator around it. Callers depend on IOrderRepository and never learn
/// there is a cache, so caching is added or removed by changing this file
/// rather than by touching every call site.
///
/// The trap is registering both the concrete type and the interface as ordinary
/// registrations. That produces two independent objects -- the decorator wraps
/// one, callers get the other -- so the cache fills up and nothing ever reads
/// it. Nothing fails; the decoration simply does nothing, which is why the test
/// asserts on which type comes back rather than only on the answer.
///
/// The built-in container has no Decorate method. Third-party containers do,
/// and for more than a couple of layers they are worth it; this shape is what
/// they generate.
/// </summary>
public static class DecoratedRepository
{
    public static IServiceCollection Register(IServiceCollection services, CallLog log)
    {
        services.AddSingleton(log);
        services.AddSingleton<SqlOrderRepository>();
        services.AddSingleton<IOrderRepository>(provider => new CachingOrderRepository(
            provider.GetRequiredService<SqlOrderRepository>(),
            provider.GetRequiredService<CallLog>()));
        services.AddSingleton<OrderLookup>();

        return services;
    }
}
