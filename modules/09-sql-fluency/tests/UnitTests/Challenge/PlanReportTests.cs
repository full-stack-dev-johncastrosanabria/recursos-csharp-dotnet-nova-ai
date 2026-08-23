using Shouldly;
using Training.Module09.Challenge;
using Training.Module09.Core;

namespace Training.Module09.Tests.Challenge;

public sealed class PlanReportTests
{
    [Fact]
    public void The_real_world_case_reports_a_full_table_read()
    {
        var findings = Analyse(Plans.FunctionWrapped);

        findings.ShouldContain(finding =>
            finding.Kind == FindingKind.SequentialScanOnLargeRelation
            && finding.Subject == "orders"
            && finding.Magnitude == 200_000);
    }

    [Fact]
    public void A_healthy_plan_reports_nothing_at_all()
    {
        // An analyser that always finds something is one nobody reads.
        Analyse(Plans.Direct).ShouldBeEmpty();
    }

    [Fact]
    public void An_unstatisticked_expression_reports_the_bad_estimate()
    {
        var findings = Analyse(Plans.BadEstimate);

        findings.ShouldContain(finding => finding.Kind == FindingKind.EstimateFarFromActual);
    }

    [Fact]
    public void A_per_row_subquery_reports_the_repetition()
    {
        var findings = Analyse(Plans.PerRowSubquery);

        findings.ShouldContain(finding =>
            finding.Kind == FindingKind.WorkRepeatedManyTimes && finding.Magnitude == 200);
    }

    [Fact]
    public void Findings_arrive_worst_first()
    {
        var findings = Analyse(Plans.BadEstimate);

        findings.Count.ShouldBeGreaterThan(1);
        findings.Select(finding => finding.Magnitude).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public void The_range_rewrite_is_clean_too()
    {
        Analyse(Plans.Range).ShouldBeEmpty();
    }

    [Fact]
    public void A_small_sequential_scan_is_not_worth_reporting()
    {
        // customers is read sequentially in the subquery plan, but only 400
        // rows of it. Sequential is not a synonym for wrong.
        var findings = Analyse(Plans.PerRowSubquery);

        findings.ShouldNotContain(finding =>
            finding.Kind == FindingKind.SequentialScanOnLargeRelation);
    }

    private static IReadOnlyList<Finding> Analyse(string fixture)
        => PlanReport.Analyse(PlanTree.Parse(Plans.Load(fixture)));
}
