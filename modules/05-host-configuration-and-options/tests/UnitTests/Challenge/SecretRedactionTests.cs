using Microsoft.Extensions.Configuration;
using Shouldly;
using Training.Module05.Challenge;

namespace Training.Module05.Tests.Challenge;

public sealed class SecretRedactionTests
{
    private static IConfiguration Configuration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Endpoint"] = "https://payments.internal",
                ["Payments:ApiKey"] = "sk_live_9f3a2b",
                ["Database:ConnectionString"] = "Host=db;Password=hunter2",
                ["Notifications:WebhookSecret"] = "whsec_abc",
                ["Notifications:RetryCount"] = "3",
                ["Logging:LogLevel:Default"] = "Information",
            })
            .Build();

    [Fact]
    public void Ordinary_values_are_shown()
    {
        var dump = SecretRedaction.Dump(Configuration());

        dump["Payments:Endpoint"].ShouldBe("https://payments.internal");
        dump["Notifications:RetryCount"].ShouldBe("3");
    }

    [Fact]
    public void A_key_named_like_a_secret_is_redacted()
    {
        var dump = SecretRedaction.Dump(Configuration());

        dump["Payments:ApiKey"].ShouldBe("***");
        dump["Notifications:WebhookSecret"].ShouldBe("***");
    }

    [Fact]
    public void Connection_strings_are_redacted_because_they_carry_passwords()
    {
        var dump = SecretRedaction.Dump(Configuration());

        dump["Database:ConnectionString"].ShouldBe("***");
    }

    [Fact]
    public void Matching_ignores_case_and_position_in_the_key()
    {
        // A startup dump that logs configuration is genuinely useful and is
        // also the most common way a production secret reaches a log
        // aggregator, a support ticket and a screenshot.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Some:apikey"] = "a",
                ["Other:CLIENT_SECRET"] = "b",
                ["Third:AuthToken"] = "c",
                ["Fourth:passwordHash"] = "d",
            })
            .Build();

        var dump = SecretRedaction.Dump(configuration);

        dump.Values.ShouldAllBe(v => v == "***");
    }

    [Fact]
    public void Every_leaf_key_appears_exactly_once()
    {
        var dump = SecretRedaction.Dump(Configuration());

        dump.Count.ShouldBe(6);
        dump.Keys.ShouldContain("Logging:LogLevel:Default");
    }

    [Fact]
    public void Intermediate_sections_are_not_listed_as_values()
    {
        // GetChildren is recursive by shape: "Payments" is a section with no
        // value of its own. Emitting it as an empty entry makes the dump noisy
        // and hides the leaves that matter.
        var dump = SecretRedaction.Dump(Configuration());

        dump.Keys.ShouldNotContain("Payments");
        dump.Keys.ShouldNotContain("Logging:LogLevel");
    }

    [Fact]
    public void An_empty_configuration_produces_an_empty_dump()
    {
        SecretRedaction.Dump(new ConfigurationBuilder().Build()).ShouldBeEmpty();
    }
}
