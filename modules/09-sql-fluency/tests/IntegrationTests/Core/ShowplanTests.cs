using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.IntegrationTests.Core;

[Collection(SharedSqlServer.Name)]
[Trait("Category", "Integration")]
public sealed class ShowplanTests(SqlServerOrders database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_function_wrapped_predicate_scans_on_SQL_Server_too()
    {
        var plan = await database.ActualPlanAsync(
            "SELECT id FROM dbo.orders WHERE LOWER(customer_email) = 'user123@example.com'", Token);

        Showplan.UsedSeek(plan).ShouldBeFalse();
        Showplan.Operators(plan).ShouldContain(op => op.PhysicalOp == "Index Scan");
    }

    [Fact]
    public async Task And_the_sargable_form_seeks_for_a_fraction_of_the_pages()
    {
        var scanned = await database.ActualPlanAsync(
            "SELECT id FROM dbo.orders WHERE LOWER(customer_email) = 'user123@example.com'", Token);
        var sought = await database.ActualPlanAsync(
            "SELECT id FROM dbo.orders WHERE customer_email = 'User123@Example.com'", Token);

        Showplan.UsedSeek(sought).ShouldBeTrue();
        Showplan.TotalLogicalReads(sought)
            .ShouldBeLessThan(Showplan.TotalLogicalReads(scanned) / 10);
    }

    [Fact]
    public async Task The_optimiser_names_the_index_it_wishes_existed()
    {
        // total_cents has no index at all.
        var plan = await database.ActualPlanAsync(
            "SELECT id, customer_email FROM dbo.orders WHERE total_cents = 5000", Token);

        Showplan.MissingIndexColumns(plan).ShouldContain("total_cents");
    }

    [Fact]
    public async Task A_query_that_needs_nothing_is_offered_nothing()
    {
        var plan = await database.ActualPlanAsync(
            "SELECT id FROM dbo.orders WHERE customer_email = 'User123@Example.com'", Token);

        Showplan.MissingIndexColumns(plan).ShouldBeEmpty();
    }
}
