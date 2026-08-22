using Shouldly;
using Training.Module02.Core;

namespace Training.Module02.Tests.Core;

public sealed class BoundedOrderCacheTests
{
    private static OrderSummary Order(string id) => new(id, 10m);

    [Fact]
    public void Stores_and_returns_an_order()
    {
        var cache = new BoundedOrderCache(3);

        cache.Put("ord_1", Order("ord_1"));

        cache.TryGet("ord_1", out var found).ShouldBeTrue();
        found!.OrderId.ShouldBe("ord_1");
    }

    [Fact]
    public void Never_grows_past_its_capacity()
    {
        // This is the whole module in one assertion. The unbounded version of
        // this cache is the real-world case; nothing about it looks wrong.
        var cache = new BoundedOrderCache(3);

        for (var i = 0; i < 100; i++)
        {
            cache.Put($"ord_{i}", Order($"ord_{i}"));
        }

        cache.Count.ShouldBe(3);
    }

    [Fact]
    public void Evicts_the_least_recently_used_entry()
    {
        var cache = new BoundedOrderCache(2);

        cache.Put("a", Order("a"));
        cache.Put("b", Order("b"));
        cache.Put("c", Order("c"));

        cache.TryGet("a", out _).ShouldBeFalse();
        cache.TryGet("b", out _).ShouldBeTrue();
        cache.TryGet("c", out _).ShouldBeTrue();
    }

    [Fact]
    public void Reading_an_entry_makes_it_recently_used()
    {
        var cache = new BoundedOrderCache(2);

        cache.Put("a", Order("a"));
        cache.Put("b", Order("b"));
        cache.TryGet("a", out _).ShouldBeTrue();
        cache.Put("c", Order("c"));

        cache.TryGet("a", out _).ShouldBeTrue();
        cache.TryGet("b", out _).ShouldBeFalse();
    }

    [Fact]
    public void Overwriting_an_existing_key_does_not_grow_the_cache()
    {
        var cache = new BoundedOrderCache(2);

        cache.Put("a", Order("a"));
        cache.Put("a", new OrderSummary("a", 99m));

        cache.Count.ShouldBe(1);
        cache.TryGet("a", out var found).ShouldBeTrue();
        found!.Total.ShouldBe(99m);
    }

    [Fact]
    public void A_missing_key_reports_a_miss_rather_than_throwing()
    {
        var cache = new BoundedOrderCache(2);

        cache.TryGet("nothing", out var found).ShouldBeFalse();
        found.ShouldBeNull();
    }
}
