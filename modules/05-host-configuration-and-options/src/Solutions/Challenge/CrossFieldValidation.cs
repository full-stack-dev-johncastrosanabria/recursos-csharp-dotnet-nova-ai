using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Training.Module05.Challenge;

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    [Range(1, 60_000)]
    public int InitialDelayMs { get; set; } = 100;

    [Range(1, 600_000)]
    public int MaxDelayMs { get; set; } = 30_000;

    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 3;
}

/// <summary>
/// Validates a rule that spans two properties.
///
/// DataAnnotations cannot express this. Each attribute sees one property, so
/// "the ceiling must not be below the floor" has nowhere to live -- and without
/// it the service starts with a retry policy that can never wait as long as it
/// was configured to, which looks like the policy simply not working.
///
/// Both validators are registered, and OptionsFactory runs all of them and
/// aggregates their failures into a single exception. That is why a three-
/// mistake configuration reports three problems at once instead of turning
/// into three deploys.
/// </summary>
public static class CrossFieldValidation
{
    public static IServiceCollection AddRetryOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RetryOptions>()
            .Bind(configuration.GetSection(RetryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<RetryOptions>, RetryOptionsConsistency>();

        return services;
    }

    private sealed class RetryOptionsConsistency : IValidateOptions<RetryOptions>
    {
        public ValidateOptionsResult Validate(string? name, RetryOptions options)
        {
            var failures = new List<string>();

            if (options.MaxDelayMs < options.InitialDelayMs)
            {
                failures.Add(
                    $"MaxDelayMs ({options.MaxDelayMs}) must be at least InitialDelayMs "
                    + $"({options.InitialDelayMs}); a ceiling below the floor means the policy never backs off.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
