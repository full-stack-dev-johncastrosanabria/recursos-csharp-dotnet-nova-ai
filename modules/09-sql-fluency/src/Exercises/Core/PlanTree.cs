
namespace Training.Module09.Core;

/// <summary>
/// One node of a PostgreSQL execution plan.
///
/// Given rather than asked for: this is the shape, not the lesson. What matters
/// is which fields are here. A plan node tells you what it did (NodeType), to
/// what (RelationName, IndexName), how many rows the planner EXPECTED
/// (PlanRows), how many it actually got (ActualRows), and how many times the
/// whole node ran (ActualLoops). Almost every diagnosis in this module is a
/// comparison between two of those numbers.
///
/// One thing to know before you use them: ActualRows and ActualTotalTimeMs are
/// PER LOOP, averaged across loops. A node with 200 loops and 4 rows produced
/// 800 rows and took 200 times its reported time. Reading those two figures as
/// totals is the single most common mistake made with EXPLAIN output.
///
/// RowsRemovedByFilter is the other number that matters and the one people
/// never look at. A scan returning 2 rows having removed 99,998 read all
/// 100,000 of them.
/// </summary>
public sealed class PlanNode
{
    public required string NodeType { get; init; }

    public string? RelationName { get; init; }

    public string? IndexName { get; init; }

    public double PlanRows { get; init; }

    public double ActualRows { get; init; }

    public int ActualLoops { get; init; }

    public double RowsRemovedByFilter { get; init; }

    public double ActualTotalTimeMs { get; init; }

    public IReadOnlyList<PlanNode> Children { get; init; } = [];
}

/// <summary>
/// Exercise: read a plan.
///
/// EXPLAIN (FORMAT JSON) returns an array with one entry, whose "Plan" property
/// is the root node. Every node may carry a "Plans" array of children, and the
/// tree is executed from the leaves up: children produce rows, parents consume
/// them.
///
/// The JSON property names are PostgreSQL's, with spaces: "Node Type",
/// "Relation Name", "Index Name", "Plan Rows", "Actual Rows", "Actual Loops",
/// "Actual Total Time". A node missing one of them simply did not report it.
///
/// Parse builds the tree. Walk yields the root and every descendant,
/// depth-first, each parent before its own children.
/// </summary>
public static class PlanTree
{
    public static PlanNode Parse(string explainJson) => throw new NotImplementedException();

    public static IEnumerable<PlanNode> Walk(PlanNode root) => throw new NotImplementedException();
}
