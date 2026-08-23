using Training.Module09.Core;

namespace Training.Module09.Challenge;

/// <summary>The three things worth saying about a plan without being asked.</summary>
public enum FindingKind
{
    SequentialScanOnLargeRelation,
    EstimateFarFromActual,
    WorkRepeatedManyTimes,
}

/// <summary>One thing wrong with a plan, and how badly.</summary>
public sealed record Finding(FindingKind Kind, string Subject, double Magnitude);

/// <summary>
/// Challenge: the capstone. Read a plan the way you would in a review, and say
/// what is wrong with it without being told what to look for.
///
/// Everything you need is in the Core exercises; this one composes them. Report,
/// ordered by Magnitude with the worst first:
///
///   SequentialScanOnLargeRelation, once per relation read sequentially having
///   examined at least LargeRelationRows rows. Subject is the relation,
///   Magnitude is the rows examined.
///
///   EstimateFarFromActual, once for the worst node whose actual row count is
///   at least EstimateRatioThreshold times its estimate. Subject is the node
///   type, Magnitude is the ratio.
///
///   WorkRepeatedManyTimes, once for the costliest node looping at least
///   LoopThreshold times. Subject is the node type, Magnitude is the loop count.
///
/// A plan with nothing wrong reports nothing. That matters as much as the rest:
/// an analyser that always finds something is one nobody reads.
/// </summary>
public static class PlanReport
{
    public const double LargeRelationRows = 10_000;

    public const double EstimateRatioThreshold = 10;

    public const int LoopThreshold = 10;

    public static IReadOnlyList<Finding> Analyse(PlanNode root) => throw new NotImplementedException();
}
