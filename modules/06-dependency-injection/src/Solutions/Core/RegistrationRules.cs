using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Training.Module06.Core;

public interface INotifier
{
    string Channel { get; }
}

public sealed class EmailNotifier : INotifier
{
    public string Channel => "email";
}

public sealed class SmsNotifier : INotifier
{
    public string Channel => "sms";
}

/// <summary>Deliberately never registered, so tests can contrast present with absent.</summary>
public interface IAuditSink
{
    string Destination { get; }
}

public sealed class FallbackNotifier : INotifier
{
    public string Channel => "fallback";
}

/// <summary>
/// Registration is a list, not a map.
///
/// Add appends. Registering the same service type twice does not replace and
/// does not warn: both entries live in the collection, a single resolve returns
/// the last one, and resolving IEnumerable returns every one in registration
/// order. That is deliberate -- it is what makes handler and middleware
/// collections work -- but it means a stray second registration silently wins
/// over the one you are reading, and nothing anywhere reports it.
///
/// TryAdd is the counterpart: register only if this service type has nothing
/// yet. It is how a library ships a default without overruling the application,
/// and it is why AddX() extension methods are safe to call twice.
///
/// Worth knowing: TryAdd keys on the *service* type alone, so it will not add a
/// second implementation of INotifier even if you wanted one. For collections
/// there is TryAddEnumerable, which keys on the implementation type instead.
/// </summary>
public static class RegistrationRules
{
    public static IServiceCollection RegisterBoth(IServiceCollection services)
    {
        services.AddSingleton<INotifier, EmailNotifier>();
        services.AddSingleton<INotifier, SmsNotifier>();

        return services;
    }

    public static IServiceCollection RegisterDefaultsWithoutOverriding(IServiceCollection services)
    {
        services.AddSingleton<INotifier, EmailNotifier>();
        services.TryAddSingleton<INotifier, FallbackNotifier>();

        return services;
    }

    public static IServiceCollection RegisterDefaultOnly(IServiceCollection services)
    {
        services.TryAddSingleton<INotifier, FallbackNotifier>();

        return services;
    }
}
