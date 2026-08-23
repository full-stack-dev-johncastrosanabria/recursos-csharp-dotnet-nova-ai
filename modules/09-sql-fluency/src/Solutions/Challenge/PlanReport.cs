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

/// <summary>Reading a plan the way you would in a review, by composing the Core checks.</summary>
public static class PlanReport
{
    public const double LargeRelationRows = 10_000;

    public const double EstimateRatioThreshold = 10;

    public const int LoopThreshold = 10;

    public static IReadOnlyList<Finding> Analyse(PlanNode root)
    {
        var findings = new List<Finding>();

        findings.AddRange(ScanStrategy.Describe(root)
            .Where(access => access.Method == AccessMethod.SequentialScan
                && access.RowsExamined >= LargeRelationRows)
            .Select(access => new Finding(
                FindingKind.SequentialScanOnLargeRelation, access.Relation, access.RowsExamined)));

        var miss = EstimateAccuracy.Worst(root);
        if (miss is not null && miss.Ratio >= EstimateRatioThreshold)
        {
            findings.Add(new Finding(FindingKind.EstimateFarFromActual, miss.NodeType, miss.Ratio));
        }

        var repeated = LoopAmplification.WorstRepeated(root, LoopThreshold);
        if (repeated is not null)
        {
            findings.Add(new Finding(
                FindingKind.WorkRepeatedManyTimes, repeated.NodeType, repeated.Loops));
        }

        return findings.OrderByDescending(finding => finding.Magnitude).ToArray();
    }
}
