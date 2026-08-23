using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class LoopAmplificationTests
{
    [Fact]
    public void A_correlated_subquery_runs_once_per_outer_row()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.PerRowSubquery));

        var worst = LoopAmplification.WorstRepeated(root, minimumLoops: 2);

        worst.ShouldNotBeNull();
        worst.Loops.ShouldBe(200);
    }

    [Fact]
    public void And_its_row_count_is_two_hundred_times_what_the_plan_prints()
    {
        // The node reads "rows=4". It produced 800.
        var root = PlanTree.Parse(Plans.Load(Plans.PerRowSubquery));
        var scan = PlanTree.Walk(root).Single(node => node.NodeType == "Index Only Scan");

        var work = LoopAmplification.Measure(scan);

        work.TotalRows.ShouldBe(800);
        work.TotalMs.ShouldBe(scan.ActualTotalTimeMs * 200, tolerance: 0.001);
    }

    [Fact]
    public void A_node_that_ran_once_is_its_own_total()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Direct));

        var work = LoopAmplification.Measure(root);

        work.Loops.ShouldBe(1);
        work.TotalMs.ShouldBe(work.PerLoopMs);
        work.TotalRows.ShouldBe(root.ActualRows);
    }

    [Fact]
    public void A_plan_with_nothing_repeated_reports_nothing()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Direct));

        LoopAmplification.WorstRepeated(root, minimumLoops: 2).ShouldBeNull();
    }

    [Fact]
    public void Parallel_workers_count_as_loops_too()
    {
        // A parallel sequential scan reports per-worker figures, so the same
        // multiplication applies: two loops, half the rows each.
        var root = PlanTree.Parse(Plans.Load(Plans.FunctionWrapped));
        var scan = PlanTree.Walk(root).Single(node => node.NodeType == "Seq Scan");

        LoopAmplification.Measure(scan).TotalRows.ShouldBe(4);
    }
}
