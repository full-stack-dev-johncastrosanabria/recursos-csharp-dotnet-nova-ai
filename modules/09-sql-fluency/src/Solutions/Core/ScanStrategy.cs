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

/// <summary>How the query reached each table, and what it read to get there.</summary>
public static class ScanStrategy
{
    public static IReadOnlyList<RelationAccess> Describe(PlanNode root)
        => PlanTree.Walk(root)
            .Where(node => MethodOf(node.NodeType) is not null && node.RelationName is not null)
            .Select(node => new RelationAccess(
                node.RelationName!,
                MethodOf(node.NodeType)!.Value,
                node.IndexName ?? BitmapIndexNameOf(node),
                node.ActualRows * node.ActualLoops,
                (node.ActualRows + node.RowsRemovedByFilter) * node.ActualLoops))
            .ToArray();

    public static IReadOnlyList<string> RelationsReadSequentially(PlanNode root)
        => Describe(root)
            .Where(access => access.Method == AccessMethod.SequentialScan)
            .Select(access => access.Relation)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static AccessMethod? MethodOf(string nodeType) => nodeType switch
    {
        "Seq Scan" => AccessMethod.SequentialScan,
        "Index Scan" => AccessMethod.IndexScan,
        "Index Only Scan" => AccessMethod.IndexOnlyScan,
        "Bitmap Heap Scan" => AccessMethod.BitmapScan,
        _ => null,
    };

    // A bitmap scan is two nodes: the heap scan names the table, and its child
    // bitmap index scan names the index.
    private static string? BitmapIndexNameOf(PlanNode node)
        => node.Children
            .FirstOrDefault(child => child.NodeType == "Bitmap Index Scan")?.IndexName;
}
