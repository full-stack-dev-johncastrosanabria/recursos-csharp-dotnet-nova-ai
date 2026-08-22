using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Training.Module05.Core;

public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    [Required]
    public string ApiKey { get; set; } = "";

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Registers the payment options.
///
/// Three calls, and the third is the one that matters. Bind attaches the
/// section. ValidateDataAnnotations turns the attributes above into checks.
/// ValidateOnStart decides *when* those checks run.
///
/// Without ValidateOnStart, options are validated lazily on first access. A
/// broken configuration is therefore accepted at boot, the instance reports
/// healthy, and the failure arrives on the first request that happens to need
/// payments -- hours later, in front of a customer, on one instance out of six,
/// with a stack trace describing the request rather than the deploy.
///
/// SectionName lives on the options type so the string is written once. A
/// section name repeated at call sites is a rename waiting to go wrong, and
/// binding a section that does not exist does not fail -- it produces defaults.
/// </summary>
public static class PaymentOptionsSetup
{
    public static IServiceCollection AddPaymentOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PaymentOptions>()
            .Bind(configuration.GetSection(PaymentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
