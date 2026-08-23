namespace Training.Module09.Core;

/// <summary>What a repeated node actually cost, once loops are accounted for.</summary>
public sealed record RepeatedWork(string NodeType, int Loops, double PerLoopMs, double TotalMs, double TotalRows);

/// <summary>
/// Exercise: multiply by the loop count.
///
/// EXPLAIN reports ActualRows and ActualTotalTime PER LOOP, averaged. A node
/// showing "rows=4 loops=200 actual time=0.01" produced 800 rows and spent two
/// milliseconds, not four rows and a hundredth of one. Read those as totals and
/// an index lookup repeated two hundred times looks like the cheapest thing in
/// the plan.
///
/// That shape has a name outside SQL: it is the N+1 query, and in a plan it
/// looks like a small, fast, perfectly indexed node with a large loop count.
/// The fix is never to make the inner lookup faster. It is to stop doing it per
/// row -- a join, or a single query with an IN list.
///
/// Measure reports one node with the multiplication done. WorstRepeated returns
/// the node with the highest TotalMs among those looping at least
/// minimumLoops times, or null if none qualifies.
/// </summary>
public static class LoopAmplification
{
    public static RepeatedWork Measure(PlanNode node) => throw new NotImplementedException();

    public static RepeatedWork? WorstRepeated(PlanNode root, int minimumLoops)
        => throw new NotImplementedException();
}
