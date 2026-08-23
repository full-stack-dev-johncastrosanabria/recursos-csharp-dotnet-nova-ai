using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.IntegrationTests.Core;

[Collection(SharedOrdersDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class SargabilityTests(OrdersDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("customer_email = 'User123@Example.com'")]
    [InlineData("placed_at >= timestamptz '2025-06-15 00:00:00+00' AND placed_at < timestamptz '2025-06-16 00:00:00+00'")]
    public async Task A_sargable_and_selective_predicate_is_served_by_the_index(string predicate)
    {
        // The rule in the unit tier is a claim about PostgreSQL. This asserts
        // the claim against PostgreSQL.
        Sargability.CanUsePlainIndex(predicate.Split(" AND ")[0]).ShouldBeTrue();

        var plan = await database.ExplainAsync($"SELECT id FROM orders WHERE {predicate}", Token);

        ScanStrategy.RelationsReadSequentially(PlanTree.Parse(plan)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("lower(customer_email) = 'user123@example.com'")]
    [InlineData("placed_at::date = date '2025-06-15'")]
    [InlineData("total_cents % 4 = 1")]
    public async Task An_unsargable_predicate_always_falls_back_to_a_scan(string predicate)
    {
        // This direction is absolute. With no expression index, the value the
        // predicate asks about is not in any index, so there is nothing to
        // choose between.
        Sargability.CanUsePlainIndex(predicate).ShouldBeFalse();

        var plan = await database.ExplainAsync($"SELECT id FROM orders WHERE {predicate}", Token);

        ScanStrategy.RelationsReadSequentially(PlanTree.Parse(plan)).ShouldBe(["orders"]);
    }

    [Fact]
    public async Task But_sargable_does_not_mean_the_index_will_be_used()
    {
        // The other half of the truth, and the half most treatments omit.
        // This predicate is perfectly sargable and matches most of the table.
        // Reading 40,000 rows through an index means 40,000 random heap
        // fetches; reading them sequentially is cheaper, so the planner is
        // right to scan. Sargability makes the index POSSIBLE. Selectivity
        // decides whether it is worth it.
        const string predicate = "placed_at >= timestamptz '2025-01-01 00:00:00+00'";

        Sargability.CanUsePlainIndex(predicate).ShouldBeTrue();

        var plan = await database.ExplainAsync($"SELECT id FROM orders WHERE {predicate}", Token);

        ScanStrategy.RelationsReadSequentially(PlanTree.Parse(plan)).ShouldBe(["orders"]);
    }
}
