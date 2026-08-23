using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.Tests.Core;

public sealed class LockOrderingTests
{
    [Fact]
    public void Ordering_is_deterministic_so_strangers_agree()
    {
        LockOrdering.Order(["WIDGET-2", "WIDGET-1", "GADGET-9"])
            .ShouldBe(["GADGET-9", "WIDGET-1", "WIDGET-2"]);
    }

    [Fact]
    public void And_drops_duplicates()
    {
        LockOrdering.Order(["A", "B", "A"]).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void Opposite_order_over_the_same_two_rows_is_the_classic_deadlock()
    {
        LockOrdering.CouldDeadlock(["A", "B"], ["B", "A"]).ShouldBeTrue();
    }

    [Fact]
    public void The_same_order_cannot_cycle()
    {
        LockOrdering.CouldDeadlock(["A", "B"], ["A", "B"]).ShouldBeFalse();
    }

    [Fact]
    public void Transactions_sharing_nothing_cannot_deadlock_with_each_other()
    {
        LockOrdering.CouldDeadlock(["A", "B"], ["C", "D"]).ShouldBeFalse();
    }

    [Fact]
    public void A_disagreement_anywhere_in_a_longer_sequence_is_enough()
    {
        LockOrdering.CouldDeadlock(["A", "B", "C"], ["C", "A"]).ShouldBeTrue();
    }

    [Fact]
    public void Sorting_both_sequences_first_removes_the_possibility()
    {
        // The whole technique, in one assertion.
        var first = LockOrdering.Order(["B", "A"]);
        var second = LockOrdering.Order(["A", "B"]);

        LockOrdering.CouldDeadlock(first, second).ShouldBeFalse();
    }

    [Fact]
    public void Ordering_does_not_help_if_only_one_side_does_it()
    {
        // Which is why this is a convention, not a local fix: every writer has
        // to follow it, including the batch job nobody remembered.
        LockOrdering.CouldDeadlock(LockOrdering.Order(["B", "A"]), ["B", "A"]).ShouldBeTrue();
    }
}
