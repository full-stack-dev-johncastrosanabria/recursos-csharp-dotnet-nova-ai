using Shouldly;
using Training.Module04.Core;

namespace Training.Module04.Tests.Core;

public sealed class SettlementRulesTests
{
    [Fact]
    public void A_paid_domestic_order_settles_immediately()
    {
        SettlementRules.Describe(new Order("a", "EU", 50m, OrderState.Paid), domesticRegion: "EU")
            .ShouldBe("settle now");
    }

    [Fact]
    public void A_large_cross_border_order_is_held_for_review()
    {
        SettlementRules.Describe(new Order("a", "US", 5_000m, OrderState.Paid), domesticRegion: "EU")
            .ShouldBe("hold for review");
    }

    [Fact]
    public void A_small_cross_border_order_settles_on_the_usual_delay()
    {
        SettlementRules.Describe(new Order("a", "US", 50m, OrderState.Paid), domesticRegion: "EU")
            .ShouldBe("settle in 3 days");
    }

    [Fact]
    public void An_unpaid_order_never_settles_whatever_else_is_true()
    {
        // Ordering in a switch expression is behaviour, not layout. Put this
        // arm below the amount checks and a large unpaid order gets held for
        // review instead of being rejected outright.
        SettlementRules.Describe(new Order("a", "EU", 5_000m, OrderState.Pending), domesticRegion: "EU")
            .ShouldBe("nothing to settle");

        SettlementRules.Describe(new Order("a", "US", 5_000m, OrderState.Cancelled), domesticRegion: "EU")
            .ShouldBe("nothing to settle");
    }

    [Fact]
    public void A_zero_amount_paid_order_is_still_nothing_to_settle()
    {
        SettlementRules.Describe(new Order("a", "EU", 0m, OrderState.Paid), domesticRegion: "EU")
            .ShouldBe("nothing to settle");
    }

    [Fact]
    public void A_negative_amount_is_a_refund_rather_than_a_settlement()
    {
        SettlementRules.Describe(new Order("a", "EU", -20m, OrderState.Paid), domesticRegion: "EU")
            .ShouldBe("refund");
    }
}
