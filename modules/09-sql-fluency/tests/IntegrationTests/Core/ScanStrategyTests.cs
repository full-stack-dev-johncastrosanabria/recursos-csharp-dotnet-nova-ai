using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.IntegrationTests.Core;

[Collection(SharedOrdersDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class ScanStrategyTests(OrdersDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_function_wrapped_predicate_reads_the_whole_table()
    {
        // Not a captured fixture: this plan was produced by PostgreSQL a
        // moment ago, against a table it has real statistics for.
        var plan = await database.ExplainAsync(
            "SELECT id FROM orders WHERE lower(customer_email) = 'user123@example.com'", Token);

        var access = ScanStrategy.Describe(PlanTree.Parse(plan)).Single();

        access.Method.ShouldBe(AccessMethod.SequentialScan);
        access.RowsExamined.ShouldBe(50_000);
    }

    [Fact]
    public async Task The_sargable_form_of_the_same_lookup_uses_the_index()
    {
        var plan = await database.ExplainAsync(
            "SELECT id FROM orders WHERE customer_email = 'User123@Example.com'", Token);

        var access = ScanStrategy.Describe(PlanTree.Parse(plan)).Single();

        access.Method.ShouldNotBe(AccessMethod.SequentialScan);
        access.RowsExamined.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task An_expression_index_rescues_the_query_without_changing_it()
    {
        const string query = "SELECT id FROM orders WHERE lower(customer_email) = 'user123@example.com'";

        await database.ExecuteAsync(
            "CREATE INDEX orders_lower_email_idx ON orders (lower(customer_email)); ANALYZE orders;",
            Token);

        try
        {
            var access = ScanStrategy.Describe(PlanTree.Parse(await database.ExplainAsync(query, Token))).Single();

            access.Method.ShouldNotBe(AccessMethod.SequentialScan);
            access.Index.ShouldBe("orders_lower_email_idx");
        }
        finally
        {
            await database.ExecuteAsync("DROP INDEX orders_lower_email_idx;", Token);
        }
    }

    [Fact]
    public async Task A_cast_to_date_scans_and_its_range_rewrite_does_not()
    {
        var cast = await database.ExplainAsync(
            "SELECT id FROM orders WHERE placed_at::date = date '2025-06-15'", Token);
        var range = await database.ExplainAsync(
            """
            SELECT id FROM orders
            WHERE placed_at >= timestamptz '2025-06-15 00:00:00+00'
              AND placed_at <  timestamptz '2025-06-16 00:00:00+00'
            """, Token);

        ScanStrategy.Describe(PlanTree.Parse(cast)).Single().Method
            .ShouldBe(AccessMethod.SequentialScan);
        ScanStrategy.Describe(PlanTree.Parse(range)).Single().Method
            .ShouldNotBe(AccessMethod.SequentialScan);
    }
}
