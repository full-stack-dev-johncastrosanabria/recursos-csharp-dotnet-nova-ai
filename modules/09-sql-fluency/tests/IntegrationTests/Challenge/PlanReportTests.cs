using Shouldly;
using Training.Module09.Challenge;
using Training.Module09.Core;

namespace Training.Module09.IntegrationTests.Challenge;

[Collection(SharedOrdersDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class PlanReportTests(OrdersDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_analyser_flags_a_live_full_table_read()
    {
        var plan = await database.ExplainAsync(
            "SELECT id FROM orders WHERE lower(customer_email) = 'user123@example.com'", Token);

        PlanReport.Analyse(PlanTree.Parse(plan)).ShouldContain(finding =>
            finding.Kind == FindingKind.SequentialScanOnLargeRelation && finding.Subject == "orders");
    }

    [Fact]
    public async Task And_says_nothing_about_a_healthy_one()
    {
        var plan = await database.ExplainAsync(
            "SELECT id FROM orders WHERE customer_email = 'User123@Example.com'", Token);

        PlanReport.Analyse(PlanTree.Parse(plan)).ShouldBeEmpty();
    }

    [Fact]
    public async Task An_expression_without_statistics_is_estimated_badly_here_too()
    {
        var plan = await database.ExplainAsync(
            "SELECT id FROM orders WHERE status = 'paid' AND total_cents % 4 = 1", Token);

        PlanReport.Analyse(PlanTree.Parse(plan)).ShouldContain(finding =>
            finding.Kind == FindingKind.EstimateFarFromActual);
    }
}
