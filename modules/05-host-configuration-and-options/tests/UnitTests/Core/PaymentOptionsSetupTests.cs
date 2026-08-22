using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Training.Module05.Core;

namespace Training.Module05.Tests.Core;

public sealed class PaymentOptionsSetupTests
{
    private static IConfiguration ConfigurationWith(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

    private static IHost HostWith(IConfiguration configuration)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddPaymentOptions(configuration);
        return builder.Build();
    }

    [Fact]
    public void Valid_configuration_binds_onto_the_options_type()
    {
        var configuration = ConfigurationWith(
            ("Payments:ApiKey", "live-key"),
            ("Payments:TimeoutSeconds", "12"),
            ("Payments:MaxRetries", "4"));

        using var host = HostWith(configuration);

        var options = host.Services.GetRequiredService<IOptions<PaymentOptions>>().Value;

        options.ApiKey.ShouldBe("live-key");
        options.TimeoutSeconds.ShouldBe(12);
        options.MaxRetries.ShouldBe(4);
    }

    [Fact]
    public void Values_absent_from_configuration_keep_the_type_s_defaults()
    {
        var configuration = ConfigurationWith(("Payments:ApiKey", "live-key"));

        using var host = HostWith(configuration);

        host.Services.GetRequiredService<IOptions<PaymentOptions>>().Value.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public async Task A_missing_required_value_fails_when_the_host_starts()
    {
        // This is the point of ValidateOnStart. Without it the same broken
        // configuration is accepted at boot and throws on the first request
        // that happens to need it -- which may be hours later, in front of a
        // customer, on one instance out of six.
        var configuration = ConfigurationWith(("Payments:TimeoutSeconds", "12"));

        using var host = HostWith(configuration);

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Message.ShouldContain("ApiKey");
    }

    [Fact]
    public async Task A_value_outside_its_allowed_range_fails_when_the_host_starts()
    {
        var configuration = ConfigurationWith(
            ("Payments:ApiKey", "live-key"),
            ("Payments:TimeoutSeconds", "0"));

        using var host = HostWith(configuration);

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Message.ShouldContain("TimeoutSeconds");
    }

    [Fact]
    public async Task A_fully_valid_configuration_starts_cleanly()
    {
        var configuration = ConfigurationWith(
            ("Payments:ApiKey", "live-key"),
            ("Payments:TimeoutSeconds", "12"));

        using var host = HostWith(configuration);

        await Should.NotThrowAsync(() => host.StartAsync(TestContext.Current.CancellationToken));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void The_section_name_is_declared_on_the_options_type_rather_than_repeated()
    {
        // A section name written as a literal at each call site is a rename
        // waiting to go wrong: binding silently produces defaults instead of
        // failing, so the service starts with an empty API key.
        var configuration = ConfigurationWith((PaymentOptions.SectionName + ":ApiKey", "live-key"));

        using var host = HostWith(configuration);

        host.Services.GetRequiredService<IOptions<PaymentOptions>>().Value.ApiKey.ShouldBe("live-key");
    }
}
