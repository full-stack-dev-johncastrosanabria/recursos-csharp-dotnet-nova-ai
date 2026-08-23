namespace Training.Module09.Core;

/// <summary>One operator in a SQL Server execution plan.</summary>
/// <param name="PhysicalOp">What it did: "Index Seek", "Index Scan", "Nested Loops".</param>
/// <param name="Index">The index it touched, without SQL Server's square brackets.</param>
/// <param name="EstimateRows">Rows the optimiser predicted.</param>
/// <param name="ActualRows">Rows it produced.</param>
/// <param name="LogicalReads">Pages it read, which is SQL Server's cost currency.</param>
public sealed record ShowplanOperator(
    string PhysicalOp,
    string? Index,
    double EstimateRows,
    double ActualRows,
    long LogicalReads);

/// <summary>
/// Exercise: read a plan from the other engine.
///
/// Every idea from the previous exercises transfers; only the packaging is
/// different. PostgreSQL hands you JSON with "Seq Scan" and "Actual Rows";
/// SQL Server hands you XML with "Index Scan" and ActualRows, and counts
/// LOGICAL READS -- pages fetched, from cache or disk -- where PostgreSQL
/// counts rows. Logical reads is the number SQL Server people tune against,
/// because it is stable: it does not move with cache warmth or machine load.
///
/// The shape, all in the showplan namespace
/// http://schemas.microsoft.com/sqlserver/2004/07/showplan:
///
///   Each operator is a RelOp element with PhysicalOp and EstimateRows.
///   Its actual figures are on RunTimeInformation/RunTimeCountersPerThread,
///   as ActualRows and ActualLogicalReads -- one element per thread, so sum
///   them. A node with no RunTimeInformation reports zero for both.
///   Its index is the first Object element beneath it that does not sit
///   beneath a NESTED RelOp; SQL Server writes the name in [brackets], and
///   Index should not.
///   A missing-index suggestion is a MissingIndex element with Column
///   children, again in [brackets].
///
/// Operators returns every RelOp in document order. UsedSeek is true when any
/// operator is a seek of some kind. TotalLogicalReads sums the whole plan.
/// MissingIndexColumns returns the suggested columns, or empty.
/// </summary>
public static class Showplan
{
    public static IReadOnlyList<ShowplanOperator> Operators(string showplanXml)
        => throw new NotImplementedException();

    public static bool UsedSeek(string showplanXml) => throw new NotImplementedException();

    public static long TotalLogicalReads(string showplanXml) => throw new NotImplementedException();

    public static IReadOnlyList<string> MissingIndexColumns(string showplanXml)
        => throw new NotImplementedException();
}
