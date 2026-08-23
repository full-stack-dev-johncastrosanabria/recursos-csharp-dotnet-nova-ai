using Shouldly;
using Training.Module11.Challenge;

namespace Training.Module11.Tests.Challenge;

public sealed class NPlusOneDetectorTests
{
    [Fact]
    public void Statements_differing_only_by_parameter_share_a_shape()
    {
        NPlusOneDetector.Normalise("SELECT * FROM \"Lines\" WHERE \"OrderId\" = @p0")
            .ShouldBe(NPlusOneDetector.Normalise("SELECT * FROM \"Lines\" WHERE \"OrderId\" = @p1"));
    }

    [Fact]
    public void So_do_statements_differing_only_by_a_literal()
    {
        NPlusOneDetector.Normalise("SELECT * FROM \"Lines\" WHERE \"OrderId\" = 7")
            .ShouldBe(NPlusOneDetector.Normalise("SELECT * FROM \"Lines\" WHERE \"OrderId\" = 812"));
        NPlusOneDetector.Normalise("SELECT * FROM \"Orders\" WHERE \"Reference\" = 'ORD-1'")
            .ShouldBe(NPlusOneDetector.Normalise("SELECT * FROM \"Orders\" WHERE \"Reference\" = 'ORD-999'"));
    }

    [Fact]
    public void Whitespace_never_makes_two_statements_look_different()
    {
        NPlusOneDetector.Normalise("SELECT  a\n  FROM b").ShouldBe("SELECT a FROM b");
    }

    [Fact]
    public void Fifty_lookups_behind_one_query_is_the_shape_it_finds()
    {
        // One query for the orders, then one per order for its lines. The log
        // of a page that took four seconds and touched nothing unusual.
        var log = new List<string> { "SELECT \"Id\" FROM \"Orders\"" };
        log.AddRange(Enumerable.Range(0, 50)
            .Select(i => $"SELECT \"Id\" FROM \"Lines\" WHERE \"OrderId\" = @p{i}"));

        var finding = NPlusOneDetector.Detect(log, threshold: 10);

        finding.ShouldNotBeNull();
        finding.Repetitions.ShouldBe(50);
        finding.Shape.ShouldContain("\"Lines\"");
    }

    [Fact]
    public void A_handful_of_repeats_is_not_a_finding()
    {
        var log = new List<string>
        {
            "SELECT a FROM b WHERE c = @p0",
            "SELECT a FROM b WHERE c = @p1",
        };

        NPlusOneDetector.Detect(log, threshold: 10).ShouldBeNull();
    }

    [Fact]
    public void Neither_is_a_log_of_genuinely_different_queries()
    {
        var log = new List<string>
        {
            "SELECT a FROM b",
            "SELECT c FROM d",
            "SELECT e FROM f",
        };

        NPlusOneDetector.Detect(log, threshold: 2).ShouldBeNull();
    }

    [Fact]
    public void An_empty_log_is_not_a_finding_either()
    {
        NPlusOneDetector.Detect([], threshold: 1).ShouldBeNull();
    }
}
