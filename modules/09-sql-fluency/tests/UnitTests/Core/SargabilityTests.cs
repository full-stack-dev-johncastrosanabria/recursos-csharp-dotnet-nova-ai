using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class SargabilityTests
{
    [Theory]
    [InlineData("customer_email = 'User1@Example.com'")]
    [InlineData("placed_at >= TIMESTAMPTZ '2025-06-15 00:00:00+00'")]
    [InlineData("total_cents <= 5000")]
    [InlineData("customer_email LIKE 'User1%'")]
    public void A_predicate_that_asks_about_the_column_itself_is_sargable(string predicate)
    {
        Sargability.Classify(predicate).ShouldBe(SargabilityVerdict.Sargable);
        Sargability.CanUsePlainIndex(predicate).ShouldBeTrue();
    }

    [Fact]
    public void A_function_around_the_column_is_not()
    {
        // The module's real-world case, in one line.
        Sargability.Classify("lower(customer_email) = 'user1@example.com'")
            .ShouldBe(SargabilityVerdict.FunctionOnColumn);
    }

    [Fact]
    public void Nor_is_a_cast()
    {
        Sargability.Classify("placed_at::date = DATE '2025-06-15'")
            .ShouldBe(SargabilityVerdict.CastOnColumn);
    }

    [Fact]
    public void Nor_is_arithmetic()
    {
        Sargability.Classify("total_cents % 4 = 1").ShouldBe(SargabilityVerdict.ArithmeticOnColumn);
    }

    [Fact]
    public void A_negated_comparison_cannot_use_an_ordered_index_either()
    {
        // An index tells you where a value IS. "Everything else" is the rest of
        // the table, which is cheaper to read sequentially.
        Sargability.Classify("status <> 'paid'").ShouldBe(SargabilityVerdict.NegatedComparison);
    }

    [Fact]
    public void A_leading_wildcard_has_no_prefix_to_search_for()
    {
        Sargability.Classify("customer_email LIKE '%example.com'")
            .ShouldBe(SargabilityVerdict.LeadingWildcard);
    }

    [Fact]
    public void The_left_hand_side_is_what_you_would_have_to_index()
    {
        Sargability.LeftHandSide("lower(customer_email) = 'x'").ShouldBe("lower(customer_email)");
        Sargability.LeftHandSide("placed_at >= TIMESTAMPTZ '2025-06-15'").ShouldBe("placed_at");
    }

    [Fact]
    public void Only_a_sargable_predicate_can_use_a_plain_index()
    {
        Sargability.CanUsePlainIndex("lower(customer_email) = 'x'").ShouldBeFalse();
        Sargability.CanUsePlainIndex("customer_email = 'x'").ShouldBeTrue();
    }
}
