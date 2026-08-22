using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Training.Module05.Challenge;

namespace Training.Module05.Tests.Challenge;

public sealed class CrossFieldValidationTests
{
    private static IHost HostWith(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddRetryOptions(configuration);
        return builder.Build();
    }

    [Fact]
    public async Task A_consistent_configuration_starts()
    {
        using var host = HostWith(
            ("Retry:InitialDelayMs", "100"),
            ("Retry:MaxDelayMs", "5000"),
            ("Retry:MaxAttempts", "4"));

        await Should.NotThrowAsync(() => host.StartAsync(TestContext.Current.CancellationToken));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_maximum_below_the_minimum_is_rejected()
    {
        // No attribute expresses this. DataAnnotations validate one property at
        // a time, so a rule relating two of them needs IValidateOptions -- and
        // without it the service starts with a retry policy that can never wait
        // as long as it was told to.
        using var host = HostWith(
            ("Retry:InitialDelayMs", "5000"),
            ("Retry:MaxDelayMs", "100"),
            ("Retry:MaxAttempts", "4"));

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Message.ShouldContain("MaxDelayMs");
    }

    [Fact]
    public async Task The_failure_message_says_which_values_conflict()
    {
        using var host = HostWith(
            ("Retry:InitialDelayMs", "5000"),
            ("Retry:MaxDelayMs", "100"),
            ("Retry:MaxAttempts", "4"));

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Message.ShouldContain("InitialDelayMs");
    }

    [Fact]
    public async Task Per_property_rules_still_apply()
    {
        using var host = HostWith(
            ("Retry:InitialDelayMs", "100"),
            ("Retry:MaxDelayMs", "5000"),
            ("Retry:MaxAttempts", "0"));

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Message.ShouldContain("MaxAttempts");
    }

    [Fact]
    public async Task Every_problem_is_reported_at_once()
    {
        // Reporting the first failure only means a three-mistake configuration
        // takes three deploys to fix.
        using var host = HostWith(
            ("Retry:InitialDelayMs", "5000"),
            ("Retry:MaxDelayMs", "100"),
            ("Retry:MaxAttempts", "0"));

        var error = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        error.Failures.Count().ShouldBeGreaterThan(1);
    }

    [Fact]
    public void The_bound_values_are_available_when_valid()
    {
        using var host = HostWith(
            ("Retry:InitialDelayMs", "250"),
            ("Retry:MaxDelayMs", "9000"),
            ("Retry:MaxAttempts", "6"));

        var options = host.Services.GetRequiredService<IOptions<RetryOptions>>().Value;

        options.InitialDelayMs.ShouldBe(250);
        options.MaxDelayMs.ShouldBe(9000);
        options.MaxAttempts.ShouldBe(6);
    }
}
