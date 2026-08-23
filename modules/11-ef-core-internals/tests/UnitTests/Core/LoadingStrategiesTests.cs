using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.Tests.Core;

public sealed class LoadingStrategiesTests
{
    [Fact]
    public void Without_an_include_the_lines_table_is_never_mentioned()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(LoadingStrategies.WithoutLines(db));

        GeneratedSql.Mentions(sql, "\"Lines\"").ShouldBeFalse();
        GeneratedSql.Occurrences(sql, "JOIN").ShouldBe(0);
    }

    [Fact]
    public void Include_brings_the_lines_back_in_the_same_round_trip()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(LoadingStrategies.Eager(db));

        GeneratedSql.Mentions(sql, "LEFT JOIN \"Lines\"").ShouldBeTrue();
        GeneratedSql.Occurrences(sql, "SELECT").ShouldBe(1);
    }

    [Fact]
    public void Include_also_orders_by_the_parent_key()
    {
        // Not decoration: EF needs the parent's rows adjacent to stitch the
        // children back onto them as it reads forward through one result set.
        using var db = ShopContext.ForSqlOnly();

        GeneratedSql.Mentions(GeneratedSql.For(LoadingStrategies.Eager(db)), "ORDER BY").ShouldBeTrue();
    }

    [Fact]
    public void A_projection_counts_in_sql_and_returns_no_entities()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(LoadingStrategies.Projected(db));

        GeneratedSql.Mentions(sql, "count(*)").ShouldBeTrue();
        GeneratedSql.Mentions(sql, "\"Total\"").ShouldBeFalse();
    }

    [Fact]
    public void And_still_only_one_query()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(LoadingStrategies.Projected(db));

        GeneratedSql.Occurrences(sql, "LEFT JOIN").ShouldBe(0);
    }
}
