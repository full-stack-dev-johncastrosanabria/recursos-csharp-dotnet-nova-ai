using Microsoft.EntityFrameworkCore;
using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.Tests.Core;

public sealed class TrackingBehaviourTests
{
    [Fact]
    public void AsNoTracking_does_not_change_the_sql_at_all()
    {
        // The decision is entirely client-side. The database cannot tell.
        using var db = ShopContext.ForSqlOnly();

        GeneratedSql.For(TrackingBehaviour.NotTracked(db))
            .ShouldBe(GeneratedSql.For(TrackingBehaviour.Tracked(db)));
    }

    [Fact]
    public void A_new_entity_is_Added_and_is_being_tracked()
    {
        using var db = ShopContext.ForSqlOnly();
        var order = new Order { Reference = "ORD-1" };

        db.Orders.Add(order);

        TrackingBehaviour.StateOf(db, order).ShouldBe(EntityState.Added);
        TrackingBehaviour.TrackedCount(db).ShouldBe(1);
    }

    [Fact]
    public void An_attached_entity_is_Unchanged_until_you_touch_it()
    {
        using var db = ShopContext.ForSqlOnly();
        var order = new Order { Id = 7, Reference = "ORD-7" };

        db.Orders.Attach(order);

        TrackingBehaviour.StateOf(db, order).ShouldBe(EntityState.Unchanged);
    }

    [Fact]
    public void Changing_a_property_on_a_tracked_entity_makes_it_Modified()
    {
        using var db = ShopContext.ForSqlOnly();
        var order = new Order { Id = 7, Reference = "ORD-7" };
        db.Orders.Attach(order);

        order.Total = 42;

        TrackingBehaviour.StateOf(db, order).ShouldBe(EntityState.Modified);
    }

    [Fact]
    public void An_untracked_instance_is_Detached()
    {
        using var db = ShopContext.ForSqlOnly();

        TrackingBehaviour.StateOf(db, new Order { Id = 99 }).ShouldBe(EntityState.Detached);
        TrackingBehaviour.TrackedCount(db).ShouldBe(0);
    }

    [Fact]
    public void The_tracker_holds_everything_until_the_context_goes_away()
    {
        using var db = ShopContext.ForSqlOnly();

        for (var i = 1; i <= 25; i++)
        {
            db.Orders.Attach(new Order { Id = i });
        }

        TrackingBehaviour.TrackedCount(db).ShouldBe(25);
    }
}
