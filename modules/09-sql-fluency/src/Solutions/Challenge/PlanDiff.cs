using Training.Module09.Core;

namespace Training.Module09.Challenge;

/// <summary>How one relation's access changed between two plans.</summary>
public sealed record AccessChange(
    string Relation,
    AccessMethod Before,
    AccessMethod After,
    double RowsExaminedBefore,
    double RowsExaminedAfter);

/// <summary>
/// Proving a change helped, by rows read rather than by wall-clock time.
/// </summary>
public static class PlanDiff
{
    public static IReadOnlyList<AccessChange> Compare(PlanNode before, PlanNode after)
    {
        var first = ByRelation(before);
        var second = ByRelation(after);

        return first.Keys
            .Where(second.ContainsKey)
            .OrderBy(relation => relation, StringComparer.Ordinal)
            .Select(relation => new AccessChange(
                relation,
                first[relation].Method,
                second[relation].Method,
                first[relation].RowsExamined,
                second[relation].RowsExamined))
            .ToArray();
    }

    public static bool IsImprovement(AccessChange change)
        => change.RowsExaminedAfter < change.RowsExaminedBefore;

    private static Dictionary<string, RelationAccess> ByRelation(PlanNode root)
        => ScanStrategy.Describe(root)
            .GroupBy(access => access.Relation, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
}
