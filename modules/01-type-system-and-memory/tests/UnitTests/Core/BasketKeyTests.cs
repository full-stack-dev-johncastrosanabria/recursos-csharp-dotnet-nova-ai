using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class BasketKeyTests
{
    private static BasketKey Basket() => new("cus_17", [new LineItem("SKU-1", 2), new LineItem("SKU-2", 1)]);

    [Fact]
    public void Two_structurally_identical_baskets_are_equal()
    {
        Basket().ShouldBe(Basket());
    }

    [Fact]
    public void Identical_baskets_hash_the_same()
    {
        Basket().GetHashCode().ShouldBe(Basket().GetHashCode());
    }

    [Fact]
    public void The_idempotency_cache_hits_on_a_retry()
    {
        // This is the whole module in one test. A retried checkout builds an
        // equal-but-not-identical key; if the cache misses, the customer is
        // charged twice.
        var cache = new Dictionary<BasketKey, string> { [Basket()] = "charge_001" };

        cache.TryGetValue(Basket(), out var chargeId).ShouldBeTrue();
        chargeId.ShouldBe("charge_001");
    }

    [Fact]
    public void A_different_quantity_is_a_different_basket()
    {
        var other = new BasketKey("cus_17", [new LineItem("SKU-1", 3), new LineItem("SKU-2", 1)]);

        Basket().ShouldNotBe(other);
    }

    [Fact]
    public void A_different_customer_is_a_different_basket()
    {
        Basket().ShouldNotBe(new BasketKey("cus_18", [new LineItem("SKU-1", 2), new LineItem("SKU-2", 1)]));
    }

    [Fact]
    public void Line_order_is_part_of_the_key()
    {
        // A deliberate design decision, not an accident: see the guide. Treating
        // a basket as a set is defensible, and more expensive to hash correctly.
        var reversed = new BasketKey("cus_17", [new LineItem("SKU-2", 1), new LineItem("SKU-1", 2)]);

        Basket().ShouldNotBe(reversed);
    }
}
