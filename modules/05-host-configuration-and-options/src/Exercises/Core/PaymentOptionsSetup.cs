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
/// Exercise: bind the section, validate it, and make the validation run when
/// the host starts rather than on first use. That last part is the whole point.
/// Without it a broken configuration is accepted at boot and throws on the
/// first request that happens to need it — hours later, in front of a customer,
/// on one instance out of six, with a stack trace that describes the request
/// rather than the deploy.
///
/// Required: ApiKey must be present. TimeoutSeconds must be between 1 and 300.
/// MaxRetries must be between 0 and 10. Values absent from configuration keep
/// the defaults declared above.
/// </summary>
public static class PaymentOptionsSetup
{
    public static IServiceCollection AddPaymentOptions(
        this IServiceCollection services,
        IConfiguration configuration)
        => throw new NotImplementedException();
}
