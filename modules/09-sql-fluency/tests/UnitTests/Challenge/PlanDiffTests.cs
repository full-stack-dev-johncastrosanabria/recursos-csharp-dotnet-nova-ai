using Shouldly;
using Training.Module09.Challenge;
using Training.Module09.Core;

namespace Training.Module09.Tests.Challenge;

public sealed class PlanDiffTests
{
    [Fact]
    public void An_expression_index_turns_the_scan_into_a_lookup()
    {
        var change = Compare(Plans.FunctionWrapped, Plans.ExpressionIndex).Single();

        change.Relation.ShouldBe("orders");
        change.Before.ShouldBe(AccessMethod.SequentialScan);
        change.After.ShouldBe(AccessMethod.BitmapScan);
    }

    [Fact]
    public void And_the_saving_is_two_hundred_thousand_rows_against_four()
    {
        var change = Compare(Plans.FunctionWrapped, Plans.ExpressionIndex).Single();

        change.RowsExaminedBefore.ShouldBe(200_000);
        change.RowsExaminedAfter.ShouldBe(4);
        PlanDiff.IsImprovement(change).ShouldBeTrue();
    }

    [Fact]
    public void Rewriting_a_cast_as_a_range_does_the_same_thing_without_an_index()
    {
        var change = Compare(Plans.CastToDate, Plans.Range).Single();

        change.Before.ShouldBe(AccessMethod.SequentialScan);
        change.After.ShouldBe(AccessMethod.BitmapScan);
        PlanDiff.IsImprovement(change).ShouldBeTrue();
    }

    [Fact]
    public void A_plan_compared_with_itself_is_not_an_improvement()
    {
        var change = Compare(Plans.Direct, Plans.Direct).Single();

        PlanDiff.IsImprovement(change).ShouldBeFalse();
    }

    [Fact]
    public void Going_the_wrong_way_is_reported_as_such()
    {
        var change = Compare(Plans.ExpressionIndex, Plans.FunctionWrapped).Single();

        change.Before.ShouldBe(AccessMethod.BitmapScan);
        change.After.ShouldBe(AccessMethod.SequentialScan);
        PlanDiff.IsImprovement(change).ShouldBeFalse();
    }

    [Fact]
    public void Only_relations_both_plans_read_are_compared()
    {
        // The join reads customers as well; the single-table plan does not.
        // Reporting that as a change would be comparing two different queries.
        var changes = Compare(Plans.HashJoin, Plans.Direct);

        changes.Select(change => change.Relation).ShouldBe(["orders"]);
    }

    private static IReadOnlyList<AccessChange> Compare(string before, string after)
        => PlanDiff.Compare(
            PlanTree.Parse(Plans.Load(before)),
            PlanTree.Parse(Plans.Load(after)));
}
