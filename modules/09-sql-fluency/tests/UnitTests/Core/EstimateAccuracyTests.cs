using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class EstimateAccuracyTests
{
    [Fact]
    public void An_expression_the_planner_has_no_statistics_for_is_guessed_badly()
    {
        // total_cents % 4 = 1 matches a quarter of the table. PostgreSQL keeps
        // statistics on columns, not on expressions over them, so it falls back
        // to a fixed guess and is wrong by more than two orders of magnitude.
        var worst = EstimateAccuracy.Worst(PlanTree.Parse(Plans.Load(Plans.BadEstimate)));

        worst.ShouldNotBeNull();
        worst.Ratio.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void A_plain_indexed_lookup_is_estimated_exactly()
    {
        var worst = EstimateAccuracy.Worst(PlanTree.Parse(Plans.Load(Plans.Direct)));

        worst.ShouldNotBeNull();
        worst.Ratio.ShouldBe(1, tolerance: 0.5);
    }

    [Fact]
    public void The_ratio_of_a_single_node_is_actual_over_planned()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Range));

        EstimateAccuracy.RatioFor(root).ShouldBe(root.ActualRows / root.PlanRows, tolerance: 0.01);
    }

    [Fact]
    public void A_zero_estimate_does_not_divide_by_zero()
    {
        var node = new PlanNode { NodeType = "Seq Scan", PlanRows = 0, ActualRows = 500 };

        EstimateAccuracy.RatioFor(node).ShouldBe(500);
    }

    [Fact]
    public void Misses_below_the_threshold_are_not_reported()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Direct));

        EstimateAccuracy.Misses(root, minimumRatio: 10).ShouldBeEmpty();
    }

    [Fact]
    public void And_misses_above_it_come_back_worst_first()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.BadEstimate));

        var misses = EstimateAccuracy.Misses(root, minimumRatio: 10);

        misses.ShouldNotBeEmpty();
        misses.Select(miss => miss.Ratio).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public void A_plan_with_no_misses_still_has_a_worst_node()
    {
        EstimateAccuracy.Worst(PlanTree.Parse(Plans.Load(Plans.Range))).ShouldNotBeNull();
    }
}
