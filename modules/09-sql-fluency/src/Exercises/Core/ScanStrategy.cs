namespace Training.Module09.Core;

/// <summary>How a relation was reached.</summary>
public enum AccessMethod
{
    SequentialScan,
    IndexScan,
    IndexOnlyScan,
    BitmapScan,
}

/// <summary>One relation, and what it cost to read it.</summary>
/// <param name="Relation">The table.</param>
/// <param name="Method">How it was reached.</param>
/// <param name="Index">The index used, if any.</param>
/// <param name="RowsReturned">Rows this scan produced, across all loops.</param>
/// <param name="RowsExamined">Rows it had to look at to produce them, across all loops.</param>
public sealed record RelationAccess(
    string Relation,
    AccessMethod Method,
    string? Index,
    double RowsReturned,
    double RowsExamined);

/// <summary>
/// Exercise: answer "how did this query reach each table, and what did it read
/// to get there".
///
/// Map node types: "Seq Scan" is SequentialScan, "Index Scan" is IndexScan,
/// "Index Only Scan" is IndexOnlyScan, and "Bitmap Heap Scan" is BitmapScan.
/// Only those four produce a RelationAccess; every other node type is
/// machinery above them.
///
/// The index name is on the node itself for the two index scans. For a bitmap
/// scan it is on the "Bitmap Index Scan" child, because the two halves are
/// separate nodes -- one builds the bitmap, the other visits the heap.
///
/// Both row counts are totals, so both are multiplied by ActualLoops.
/// RowsExamined is rows returned plus rows the filter threw away, which is the
/// number this module exists to make you look at: a scan returning 2 rows
/// having removed 99,998 read the whole table.
/// </summary>
public static class ScanStrategy
{
    public static IReadOnlyList<RelationAccess> Describe(PlanNode root)
        => throw new NotImplementedException();

    public static IReadOnlyList<string> RelationsReadSequentially(PlanNode root)
        => throw new NotImplementedException();
}
