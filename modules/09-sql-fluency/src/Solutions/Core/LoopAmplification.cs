namespace Training.Module09.Core;

/// <summary>What a repeated node actually cost, once loops are accounted for.</summary>
public sealed record RepeatedWork(string NodeType, int Loops, double PerLoopMs, double TotalMs, double TotalRows);

/// <summary>Multiplying by the loop count, because EXPLAIN reports per loop.</summary>
public static class LoopAmplification
{
    public static RepeatedWork Measure(PlanNode node)
        => new(
            node.NodeType,
            node.ActualLoops,
            node.ActualTotalTimeMs,
            node.ActualTotalTimeMs * node.ActualLoops,
            node.ActualRows * node.ActualLoops);

    public static RepeatedWork? WorstRepeated(PlanNode root, int minimumLoops)
        => PlanTree.Walk(root)
            .Where(node => node.ActualLoops >= minimumLoops)
            .Select(Measure)
            .OrderByDescending(work => work.TotalMs)
            .FirstOrDefault();
}
