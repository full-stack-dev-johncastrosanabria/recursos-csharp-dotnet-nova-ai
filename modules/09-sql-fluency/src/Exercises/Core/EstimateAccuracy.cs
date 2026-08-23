namespace Training.Module09.Core;

/// <summary>A node where the planner's guess and reality diverged.</summary>
public sealed record EstimateMiss(string NodeType, string? Relation, double PlanRows, double ActualRows, double Ratio);

/// <summary>
/// Exercise: find where the planner was wrong.
///
/// A plan is only as good as its row estimates. The planner chooses a
/// sequential scan over an index, or a hash join over a nested loop, by
/// predicting how many rows each step will produce. When that prediction is
/// wrong by two orders of magnitude, the plan it chose was reasonable for a
/// query that does not exist -- and no amount of staring at the chosen plan
/// explains it. The estimate is the explanation.
///
/// RatioFor is ActualRows divided by PlanRows, with PlanRows floored at 1 so a
/// zero estimate does not divide by zero. Both figures are per loop, so they
/// are directly comparable and neither is multiplied here.
///
/// Misses returns every node whose ratio is at or above the given threshold,
/// worst first. Worst returns the single worst, or null if the plan has none.
/// </summary>
public static class EstimateAccuracy
{
    public static double RatioFor(PlanNode node) => throw new NotImplementedException();

    public static IReadOnlyList<EstimateMiss> Misses(PlanNode root, double minimumRatio)
        => throw new NotImplementedException();

    public static EstimateMiss? Worst(PlanNode root) => throw new NotImplementedException();
}
