using Microsoft.EntityFrameworkCore;
using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.IntegrationTests.Core;

[Collection(SharedShopDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class TranslationBoundaryTests(ShopDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_sql_LINQ_generated_for_ToUpper_really_does_scan()
    {
        // Module 09's finding, reached from LINQ. There is an index on
        // "Reference"; upper("Reference") is not in it, so the planner reads
        // the table. Nothing in the C# hints at this.
        await using var db = database.Create();
        var sql = TranslationBoundary.ByUpperCase(db, "ORD-1").ToQueryString();

        var plan = await database.ExplainAsync(sql, "ORD-1", Token);

        plan.ShouldContain("Seq Scan");
    }

    [Fact]
    public async Task And_the_direct_comparison_uses_the_index()
    {
        await using var db = database.Create();
        var sql = TranslationBoundary.ByExactMatch(db, "ORD-1").ToQueryString();

        var plan = await database.ExplainAsync(sql, "ORD-1", Token);

        plan.ShouldNotContain("Seq Scan");
        plan.ShouldContain("orders_reference_idx");
    }

    [Fact]
    public async Task Both_return_the_same_row_which_is_why_nobody_notices()
    {
        await using var db = database.Create();

        var upper = await TranslationBoundary.ByUpperCase(db, "ORD-1").ToListAsync(Token);
        var exact = await TranslationBoundary.ByExactMatch(db, "ORD-1").ToListAsync(Token);

        upper.Count.ShouldBe(1);
        exact.Count.ShouldBe(1);
        upper[0].Reference.ShouldBe(exact[0].Reference);
    }
}
