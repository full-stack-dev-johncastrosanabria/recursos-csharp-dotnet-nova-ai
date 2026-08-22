using Microsoft.Extensions.DependencyInjection;

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
/// Exercise: three registration shapes, each with a different meaning.
///
/// RegisterBoth registers email then sms. Registering twice does not replace
/// and does not throw -- both live in the collection, a single resolve returns
/// the last, and resolving IEnumerable returns all of them in order.
///
/// RegisterDefaultsWithoutOverriding registers email normally and then offers
/// fallback as a default that must not displace it. That is what a library does
/// when it supplies a sensible default without overruling the application.
///
/// RegisterDefaultOnly offers fallback as a default with nothing already there.
/// </summary>
public static class RegistrationRules
{
    public static IServiceCollection RegisterBoth(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterDefaultsWithoutOverriding(IServiceCollection services)
        => throw new NotImplementedException();

    public static IServiceCollection RegisterDefaultOnly(IServiceCollection services)
        => throw new NotImplementedException();
}
