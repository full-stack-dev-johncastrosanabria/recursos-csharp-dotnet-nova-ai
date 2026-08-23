using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.Tests.Core;

public sealed class SplitQueriesTests
{
    [Fact]
    public void Two_includes_in_one_query_join_both_collections()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(SplitQueries.BothCollections(db));

        GeneratedSql.Occurrences(sql, "LEFT JOIN").ShouldBe(2);
    }

    [Fact]
    public void Splitting_leaves_the_first_query_with_no_joins_at_all()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(SplitQueries.BothCollectionsSplit(db));

        GeneratedSql.Occurrences(sql, "LEFT JOIN").ShouldBe(0);
        GeneratedSql.Mentions(sql, "FROM \"Orders\"").ShouldBeTrue();
    }

    [Fact]
    public void A_single_query_pulls_the_product_of_the_two_collections()
    {
        // 100 orders with 20 lines and 10 payments each is not 3,100 rows.
        SplitQueries.RowsFromSingleQuery(100, 20, 10).ShouldBe(20_000);
    }

    [Fact]
    public void A_split_query_pulls_their_sum()
    {
        SplitQueries.RowsFromSplitQuery(100, 20, 10).ShouldBe(3_100);
    }

    [Fact]
    public void The_gap_widens_with_every_extra_child_row()
    {
        // Doubling the lines doubles the split cost and doubles the single-query
        // cost too -- but from a number that was already an order of magnitude
        // larger, which is why this is discovered as an out-of-memory error.
        var single = SplitQueries.RowsFromSingleQuery(100, 40, 10);
        var split = SplitQueries.RowsFromSplitQuery(100, 40, 10);

        single.ShouldBe(40_000);
        split.ShouldBe(5_100);
    }

    [Fact]
    public void An_order_with_no_children_still_returns_one_row()
    {
        SplitQueries.RowsFromSingleQuery(10, 0, 0).ShouldBe(10);
    }
}
