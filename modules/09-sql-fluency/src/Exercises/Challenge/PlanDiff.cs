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
/// Challenge: prove the change helped.
///
/// "It looks faster" is not a finding -- timings move with cache warmth, with
/// load, and with whoever else is on the box. What does not move is how much
/// the database had to read, and that is what a before-and-after comparison
/// should be about.
///
/// Compare reports one AccessChange per relation that BOTH plans read, ordered
/// by relation name. A relation only one of them touched is not a change worth
/// reporting -- the query is different, not faster.
///
/// IsImprovement is true when the second plan examined strictly fewer rows.
/// </summary>
public static class PlanDiff
{
    public static IReadOnlyList<AccessChange> Compare(PlanNode before, PlanNode after)
        => throw new NotImplementedException();

    public static bool IsImprovement(AccessChange change) => throw new NotImplementedException();
}
