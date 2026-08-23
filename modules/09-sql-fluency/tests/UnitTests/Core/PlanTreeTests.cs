using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class PlanTreeTests
{
    [Fact]
    public void The_root_of_a_sequential_scan_plan_is_read()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.FunctionWrapped));

        root.NodeType.ShouldBe("Gather");
        root.ActualRows.ShouldBe(4);
    }

    [Fact]
    public void Children_are_read_recursively()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.FunctionWrapped));

        var scan = PlanTree.Walk(root).Single(node => node.NodeType == "Seq Scan");

        scan.RelationName.ShouldBe("orders");
    }

    [Fact]
    public void An_index_scan_reports_the_index_it_used()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Direct));

        var index = PlanTree.Walk(root).Single(node => node.NodeType == "Bitmap Index Scan");

        index.IndexName.ShouldBe("orders_customer_email_idx");
    }

    [Fact]
    public void Walk_yields_every_node_parents_before_children()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.HashJoin));

        var order = PlanTree.Walk(root).Select(node => node.NodeType).ToList();

        order[0].ShouldBe(root.NodeType);
        order.Count.ShouldBeGreaterThan(3);
        order.ShouldContain("Hash Join");
    }

    [Fact]
    public void A_node_that_reported_no_relation_simply_has_none()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.HashJoin));

        PlanTree.Walk(root).First(node => node.NodeType == "Hash Join").RelationName.ShouldBeNull();
    }

    [Fact]
    public void Loop_counts_are_read()
    {
        // 200 outer rows, so the correlated subquery underneath ran 200 times.
        var root = PlanTree.Parse(Plans.Load(Plans.PerRowSubquery));

        PlanTree.Walk(root).Max(node => node.ActualLoops).ShouldBe(200);
    }
}
