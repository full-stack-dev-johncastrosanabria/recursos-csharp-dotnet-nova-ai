using Shouldly;
using Training.Module09.Challenge;

namespace Training.Module09.Tests.Challenge;

public sealed class EngineSargabilityTests
{
    [Theory]
    [InlineData("customer_email = 'User1@Example.com'")]
    [InlineData("placed_at >= timestamptz '2025-06-15'")]
    public void A_bare_column_comparison_seeks_on_both(string predicate)
    {
        EngineSargability.CanSeek(SqlEngine.PostgreSql, predicate).ShouldBeTrue();
        EngineSargability.CanSeek(SqlEngine.SqlServer, predicate).ShouldBeTrue();
        EngineSargability.EnginesAgree(predicate).ShouldBeTrue();
    }

    [Theory]
    [InlineData("lower(customer_email) = 'user1@example.com'")]
    [InlineData("YEAR(placed_at) = 2025")]
    [InlineData("total_cents % 4 = 1")]
    public void And_a_computed_column_scans_on_both(string predicate)
    {
        // The principle is universal: the index does not contain this value.
        EngineSargability.CanSeek(SqlEngine.PostgreSql, predicate).ShouldBeFalse();
        EngineSargability.CanSeek(SqlEngine.SqlServer, predicate).ShouldBeFalse();
        EngineSargability.EnginesAgree(predicate).ShouldBeTrue();
    }

    [Fact]
    public void But_a_date_cast_is_where_they_part()
    {
        // Verified against both engines: SQL Server rewrites this into a range
        // and seeks; PostgreSQL reads the whole table. The same line of SQL,
        // and the opposite outcome.
        const string tsql = "CAST(placed_at AS date) = '2025-06-15'";

        EngineSargability.CanSeek(SqlEngine.SqlServer, tsql).ShouldBeTrue();
        EngineSargability.CanSeek(SqlEngine.PostgreSql, tsql).ShouldBeFalse();
        EngineSargability.EnginesAgree(tsql).ShouldBeFalse();
    }

    [Fact]
    public void The_postgres_spelling_of_that_cast_behaves_the_same_way()
    {
        const string pg = "placed_at::date = DATE '2025-06-15'";

        EngineSargability.CanSeek(SqlEngine.SqlServer, pg).ShouldBeTrue();
        EngineSargability.CanSeek(SqlEngine.PostgreSql, pg).ShouldBeFalse();
    }

    [Fact]
    public void Agreement_is_the_question_worth_asking()
    {
        // A rule of thumb carried from one engine to the other is right most of
        // the time, which is exactly what makes the exception expensive.
        EngineSargability.EnginesAgree("customer_email LIKE '%example.com'").ShouldBeTrue();
        EngineSargability.EnginesAgree("CAST(placed_at AS date) = '2025-06-15'").ShouldBeFalse();
    }
}
