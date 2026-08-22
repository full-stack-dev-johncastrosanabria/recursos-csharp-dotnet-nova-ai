using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
/// Registers the retry options with validation that spans properties.
///
/// Challenge: MaxDelayMs must be at least InitialDelayMs. No attribute
/// expresses that — DataAnnotations validate one property at a time — so it
/// needs IValidateOptions&lt;T&gt;. Without it the service starts with a retry
/// policy whose ceiling is below its floor, and the policy quietly never backs
/// off at all.
///
/// Report every problem at once, and name the properties involved. A validator
/// that stops at the first failure turns a three-mistake configuration into
/// three deploys.
/// </summary>
public static class CrossFieldValidation
{
    public static IServiceCollection AddRetryOptions(
        this IServiceCollection services,
        IConfiguration configuration)
        => throw new NotImplementedException();
}
