using Shouldly;
using Training.Module05.Core;

namespace Training.Module05.Tests.Core;

public sealed class ConfigurationLayeringTests
{
    private static readonly Dictionary<string, string?> BaseSettings = new()
    {
        ["Payments:ApiKey"] = "base-key",
        ["Payments:TimeoutSeconds"] = "30",
        ["Payments:Endpoint"] = "https://payments.internal",
    };

    private static readonly Dictionary<string, string?> EnvironmentSettings = new()
    {
        ["Payments:TimeoutSeconds"] = "5",
    };

    private static readonly Dictionary<string, string?> ProcessSettings = new()
    {
        ["Payments:ApiKey"] = "process-key",
    };

    [Fact]
    public void A_value_present_only_in_the_base_layer_survives()
    {
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        configuration["Payments:Endpoint"].ShouldBe("https://payments.internal");
    }

    [Fact]
    public void A_later_layer_overrides_an_earlier_one()
    {
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        configuration["Payments:TimeoutSeconds"].ShouldBe("5");
    }

    [Fact]
    public void The_last_layer_wins_over_every_earlier_one()
    {
        // Precedence is registration order, last wins. Getting this backwards
        // means production reads a developer's local settings, and it looks
        // correct in every environment where the layers happen to agree.
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        configuration["Payments:ApiKey"].ShouldBe("process-key");
    }

    [Fact]
    public void A_missing_key_is_null_rather_than_an_exception()
    {
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        configuration["Payments:NotThere"].ShouldBeNull();
    }

    [Fact]
    public void Keys_are_matched_without_regard_to_case()
    {
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        configuration["payments:apikey"].ShouldBe("process-key");
    }

    [Fact]
    public void A_section_exposes_only_its_own_keys()
    {
        var configuration = ConfigurationLayering.Build(BaseSettings, EnvironmentSettings, ProcessSettings);

        var section = configuration.GetSection("Payments");

        section["ApiKey"].ShouldBe("process-key");
        section["Payments:ApiKey"].ShouldBeNull();
    }
}
