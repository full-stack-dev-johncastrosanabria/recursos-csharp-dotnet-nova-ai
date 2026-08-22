using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Training.Module06.Core;

namespace Training.Module06.Tests.Core;

public sealed class RegistrationRulesTests
{
    [Fact]
    public void Resolving_one_service_gives_the_last_registration()
    {
        // Registering twice does not replace and does not throw. Both live in
        // the collection; a single resolve returns the last. A stray extra
        // registration therefore wins silently over the one you were reading.
        var services = new ServiceCollection();
        RegistrationRules.RegisterBoth(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotifier>().Channel.ShouldBe("sms");
    }

    [Fact]
    public void Resolving_the_collection_gives_every_registration_in_order()
    {
        var services = new ServiceCollection();
        RegistrationRules.RegisterBoth(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IEnumerable<INotifier>>()
            .Select(n => n.Channel)
            .ShouldBe(["email", "sms"]);
    }

    [Fact]
    public void TryAdd_leaves_an_existing_registration_alone()
    {
        // This is how a library supplies a default without overriding whatever
        // the application already chose.
        var services = new ServiceCollection();
        RegistrationRules.RegisterDefaultsWithoutOverriding(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotifier>().Channel.ShouldBe("email");
    }

    [Fact]
    public void TryAdd_registers_when_nothing_is_there_yet()
    {
        var services = new ServiceCollection();
        RegistrationRules.RegisterDefaultOnly(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotifier>().Channel.ShouldBe("fallback");
    }

    [Fact]
    public void GetService_answers_null_for_what_is_missing_and_an_instance_for_what_is_not()
    {
        var services = new ServiceCollection();
        RegistrationRules.RegisterBoth(services);

        using var provider = services.BuildServiceProvider();

        provider.GetService<INotifier>().ShouldNotBeNull();
        provider.GetService<IAuditSink>().ShouldBeNull();
    }

    [Fact]
    public void GetRequiredService_throws_for_the_same_missing_service()
    {
        // The pair matters. GetService returning null is easy to carry past the
        // mistake; GetRequiredService fails where the mistake is.
        var services = new ServiceCollection();
        RegistrationRules.RegisterBoth(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotifier>().Channel.ShouldBe("sms");
        Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IAuditSink>());
    }

    [Fact]
    public void An_unregistered_collection_is_empty_rather_than_missing()
    {
        // IEnumerable<T> always resolves, even when nothing implements T -- so
        // it never throws and never returns null. A pipeline built from an
        // empty handler set does nothing at all and reports success doing it.
        var services = new ServiceCollection();
        RegistrationRules.RegisterBoth(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IEnumerable<INotifier>>().Count().ShouldBe(2);
        provider.GetRequiredService<IEnumerable<IAuditSink>>().ShouldBeEmpty();
    }
}
