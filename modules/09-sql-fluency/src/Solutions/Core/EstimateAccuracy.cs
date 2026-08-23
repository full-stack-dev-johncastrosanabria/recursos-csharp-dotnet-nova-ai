namespace Training.Module09.Core;

/// <summary>A node where the planner's guess and reality diverged.</summary>
public sealed record EstimateMiss(string NodeType, string? Relation, double PlanRows, double ActualRows, double Ratio);

/// <summary>Finding where the planner was wrong, which is usually the explanation.</summary>
public static class EstimateAccuracy
{
    public static double RatioFor(PlanNode node) => node.ActualRows / Math.Max(node.PlanRows, 1);

    public static IReadOnlyList<EstimateMiss> Misses(PlanNode root, double minimumRatio)
        => PlanTree.Walk(root)
            .Select(node => new EstimateMiss(
                node.NodeType, node.RelationName, node.PlanRows, node.ActualRows, RatioFor(node)))
            .Where(miss => miss.Ratio >= minimumRatio)
            .OrderByDescending(miss => miss.Ratio)
            .ToArray();

    public static EstimateMiss? Worst(PlanNode root)
    {
        var misses = Misses(root, minimumRatio: 0);

        return misses.Count > 0 ? misses[0] : null;
    }
}
