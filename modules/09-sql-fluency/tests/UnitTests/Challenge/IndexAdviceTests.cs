using Shouldly;
using Training.Module09.Challenge;

namespace Training.Module09.Tests.Challenge;

public sealed class IndexAdviceTests
{
    [Fact]
    public void A_sargable_predicate_wants_an_ordinary_index()
    {
        var advice = IndexAdvice.For("orders", "customer_email = 'User1@Example.com'");

        advice.Remedy.ShouldBe(Remedy.CreatePlainIndex);
        advice.Statement.ShouldBe("CREATE INDEX ON orders (customer_email);");
    }

    [Fact]
    public void A_function_wrapped_predicate_wants_that_function_indexed()
    {
        var advice = IndexAdvice.For("orders", "lower(customer_email) = 'user1@example.com'");

        advice.Remedy.ShouldBe(Remedy.CreateExpressionIndex);
        advice.Statement.ShouldBe("CREATE INDEX ON orders (lower(customer_email));");
    }

    [Fact]
    public void So_does_arithmetic()
    {
        var advice = IndexAdvice.For("orders", "total_cents % 4 = 1");

        advice.Remedy.ShouldBe(Remedy.CreateExpressionIndex);
        advice.Statement.ShouldBe("CREATE INDEX ON orders (total_cents % 4);");
    }

    [Fact]
    public void A_cast_to_date_is_a_query_bug_not_a_missing_index()
    {
        // The distinction worth having. An expression index would work, and it
        // would also cost something on every insert forever -- to support a
        // query that should simply have been written as a range.
        var advice = IndexAdvice.For("orders", "placed_at::date = DATE '2025-06-15'");

        advice.Remedy.ShouldBe(Remedy.RewriteAsRange);
        advice.Statement.ShouldContain("placed_at");
    }

    [Fact]
    public void A_leading_wildcard_is_beyond_a_btree()
    {
        var advice = IndexAdvice.For("orders", "customer_email LIKE '%example.com'");

        advice.Remedy.ShouldBe(Remedy.NoBTreeIndexHelps);
        advice.Statement.ShouldNotBeEmpty();
    }

    [Fact]
    public void And_so_is_a_negation()
    {
        IndexAdvice.For("orders", "status <> 'paid'").Remedy.ShouldBe(Remedy.NoBTreeIndexHelps);
    }
}
