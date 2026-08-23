using Shouldly;
using Training.Module11.Challenge;
using Training.Module11.Core;

namespace Training.Module11.Tests.Challenge;

public sealed class ChangeTrackerAuditTests
{
    [Fact]
    public void An_empty_context_has_nothing_pending()
    {
        using var db = ShopContext.ForSqlOnly();

        ChangeTrackerAudit.Summarise(db).ShouldBe(new PendingWork(0, 0, 0, []));
    }

    [Fact]
    public void A_new_entity_counts_as_an_insert()
    {
        using var db = ShopContext.ForSqlOnly();
        db.Orders.Add(new Order { Reference = "ORD-1" });

        var work = ChangeTrackerAudit.Summarise(db);

        work.Inserts.ShouldBe(1);
        work.Updates.ShouldBe(0);
    }

    [Fact]
    public void A_touched_entity_counts_as_an_update_and_names_the_column()
    {
        using var db = ShopContext.ForSqlOnly();
        var order = new Order { Id = 1, Reference = "ORD-1" };
        db.Orders.Attach(order);

        order.Total = 99;

        var work = ChangeTrackerAudit.Summarise(db);

        work.Updates.ShouldBe(1);
        work.ModifiedProperties.ShouldBe(["Total"]);
    }

    [Fact]
    public void Modified_property_names_are_sorted_and_deduplicated()
    {
        using var db = ShopContext.ForSqlOnly();
        var first = new Order { Id = 1 };
        var second = new Order { Id = 2 };
        db.Orders.Attach(first);
        db.Orders.Attach(second);

        first.Total = 1;
        first.Reference = "A";
        second.Total = 2;

        ChangeTrackerAudit.Summarise(db).ModifiedProperties.ShouldBe(["Reference", "Total"]);
    }

    [Fact]
    public void A_removed_entity_counts_as_a_delete()
    {
        using var db = ShopContext.ForSqlOnly();
        var order = new Order { Id = 1 };
        db.Orders.Attach(order);

        db.Orders.Remove(order);

        ChangeTrackerAudit.Summarise(db).Deletes.ShouldBe(1);
    }

    [Fact]
    public void An_added_entity_contributes_no_modified_properties()
    {
        // Every column is going to be written regardless, so listing them
        // would be noise rather than information.
        using var db = ShopContext.ForSqlOnly();
        db.Orders.Add(new Order { Reference = "ORD-1", Total = 5 });

        ChangeTrackerAudit.Summarise(db).ModifiedProperties.ShouldBeEmpty();
    }
}
