using Microsoft.EntityFrameworkCore;
using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.IntegrationTests.Core;

[Collection(SharedShopDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class TrackingBehaviourTests(ShopDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_re_query_returns_the_instance_the_context_already_had()
    {
        // The second half of the real-world case. Another context committed a
        // new value; this one asks again and is handed what it already held.
        await using var db = database.Create();
        var first = await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-2", Token);
        var original = first.Total;

        await using (var other = database.Create())
        {
            var same = await other.Orders.FirstAsync(o => o.Reference == "ORD-2", Token);
            same.Total = 9999;
            await other.SaveChangesAsync(Token);
        }

        var again = await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-2", Token);

        again.ShouldBeSameAs(first);
        again.Total.ShouldBe(original);
    }

    [Fact]
    public async Task And_it_ran_the_query_anyway_then_threw_the_answer_away()
    {
        // The part that makes this expensive rather than merely surprising.
        // This is not a cache avoiding a round trip: the round trip happened,
        // the fresh row came back, and EF discarded it in favour of the
        // instance it was already tracking.
        await using var db = database.Create();
        await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-3", Token);

        database.Commands.Reset();
        await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-3", Token);

        database.Commands.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AsNoTracking_sees_what_is_actually_there()
    {
        await using var db = database.Create();
        await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-4", Token);

        await using (var other = database.Create())
        {
            var same = await other.Orders.FirstAsync(o => o.Reference == "ORD-4", Token);
            same.Total = 5555;
            await other.SaveChangesAsync(Token);
        }

        var fresh = await TrackingBehaviour.NotTracked(db).FirstAsync(o => o.Reference == "ORD-4", Token);

        fresh.Total.ShouldBe(5555);
    }

    [Fact]
    public async Task Reload_updates_the_instance_you_are_holding()
    {
        await using var db = database.Create();
        var order = await TrackingBehaviour.Tracked(db).FirstAsync(o => o.Reference == "ORD-5", Token);

        await using (var other = database.Create())
        {
            var same = await other.Orders.FirstAsync(o => o.Reference == "ORD-5", Token);
            same.Total = 4242;
            await other.SaveChangesAsync(Token);
        }

        await db.Entry(order).ReloadAsync(Token);

        order.Total.ShouldBe(4242);
    }

    [Fact]
    public async Task A_tracking_query_holds_every_row_it_returned()
    {
        await using var db = database.Create();

        await TrackingBehaviour.Tracked(db).OrderBy(o => o.Id).Take(50).ToListAsync(Token);

        TrackingBehaviour.TrackedCount(db).ShouldBe(50);
    }

    [Fact]
    public async Task An_untracked_one_holds_nothing()
    {
        await using var db = database.Create();

        await TrackingBehaviour.NotTracked(db).Take(50).ToListAsync(Token);

        TrackingBehaviour.TrackedCount(db).ShouldBe(0);
    }
}
