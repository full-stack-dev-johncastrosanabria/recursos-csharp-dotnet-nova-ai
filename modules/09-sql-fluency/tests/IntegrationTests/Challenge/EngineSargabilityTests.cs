using Shouldly;
using Training.Module09.Challenge;
using Training.Module09.Core;

namespace Training.Module09.IntegrationTests.Challenge;

[Collection(SharedSqlServer.Name)]
[Trait("Category", "Integration")]
public sealed class EngineSargabilityTests(SqlServerOrders database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SQL_Server_really_does_seek_through_a_date_cast()
    {
        // The claim the unit tier encodes, asserted against the engine. The
        // giveaway in the plan is the Constant Scan: the optimiser built the
        // range itself and seeked it.
        const string predicate = "CAST(placed_at AS date) = '2025-06-15'";

        EngineSargability.CanSeek(SqlEngine.SqlServer, predicate).ShouldBeTrue();

        var plan = await database.ActualPlanAsync($"SELECT id FROM dbo.orders WHERE {predicate}", Token);

        Showplan.UsedSeek(plan).ShouldBeTrue();
        Showplan.Operators(plan).ShouldContain(op => op.PhysicalOp == "Constant Scan");
    }

    [Fact]
    public async Task And_really_does_not_for_a_year_function()
    {
        // Which is why this is a rewrite rule and not a general principle: a
        // year does not preserve enough order for the optimiser to invent a
        // range from it.
        const string predicate = "YEAR(placed_at) = 2025";

        EngineSargability.CanSeek(SqlEngine.SqlServer, predicate).ShouldBeFalse();

        var plan = await database.ActualPlanAsync($"SELECT id FROM dbo.orders WHERE {predicate}", Token);

        Showplan.UsedSeek(plan).ShouldBeFalse();
    }

    [Fact]
    public async Task Where_the_engines_agree_they_agree_for_the_same_reason()
    {
        const string predicate = "LOWER(customer_email) = 'user123@example.com'";

        EngineSargability.EnginesAgree(predicate).ShouldBeTrue();

        var plan = await database.ActualPlanAsync($"SELECT id FROM dbo.orders WHERE {predicate}", Token);

        Showplan.UsedSeek(plan).ShouldBeFalse();
    }
}
