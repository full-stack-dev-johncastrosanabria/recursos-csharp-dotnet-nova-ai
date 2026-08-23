using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.Tests.Core;

public sealed class GeneratedSqlTests
{
    [Fact]
    public void The_sql_for_a_query_is_available_without_a_database()
    {
        // Nothing is connected. The provider knows the dialect; that is enough.
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(db.Orders.Where(o => o.Total > 100));

        sql.ShouldStartWith("SELECT");
        GeneratedSql.Mentions(sql, "FROM \"Orders\"").ShouldBeTrue();
        GeneratedSql.Mentions(sql, "WHERE").ShouldBeTrue();
    }

    [Fact]
    public void Whitespace_is_collapsed_so_the_result_is_matchable()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(db.Orders);

        sql.ShouldNotContain("\n");
        sql.ShouldNotContain("  ");
        sql.Trim().ShouldBe(sql);
    }

    [Fact]
    public void A_projection_selects_only_what_it_needs()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(db.Orders.Select(o => o.Id));

        GeneratedSql.Mentions(sql, "\"Reference\"").ShouldBeFalse();
        GeneratedSql.Mentions(sql, "\"Total\"").ShouldBeFalse();
    }

    [Fact]
    public void Occurrences_counts_case_insensitively()
    {
        GeneratedSql.Occurrences("SELECT a JOIN b JOIN c", "join").ShouldBe(2);
        GeneratedSql.Occurrences("SELECT a", "join").ShouldBe(0);
    }

    [Fact]
    public void Mentions_is_the_boolean_form()
    {
        GeneratedSql.Mentions("SELECT a LEFT JOIN b", "left join").ShouldBeTrue();
        GeneratedSql.Mentions("SELECT a", "left join").ShouldBeFalse();
    }
}
