using System.Xml.Linq;

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
/// Reading a plan from the other engine. Same ideas, different packaging: XML
/// rather than JSON, seeks and scans rather than index and sequential scans,
/// and logical reads rather than rows as the currency.
/// </summary>
public static class Showplan
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    public static IReadOnlyList<ShowplanOperator> Operators(string showplanXml)
        => XDocument.Parse(showplanXml)
            .Descendants(Ns + "RelOp")
            .Select(relOp => new ShowplanOperator(
                (string)relOp.Attribute("PhysicalOp")!,
                IndexOf(relOp),
                (double?)relOp.Attribute("EstimateRows") ?? 0,
                Counters(relOp).Sum(c => (double?)c.Attribute("ActualRows") ?? 0),
                Counters(relOp).Sum(c => (long?)c.Attribute("ActualLogicalReads") ?? 0)))
            .ToArray();

    public static bool UsedSeek(string showplanXml)
        => Operators(showplanXml).Any(op => op.PhysicalOp.Contains("Seek", StringComparison.Ordinal));

    public static long TotalLogicalReads(string showplanXml)
        => Operators(showplanXml).Sum(op => op.LogicalReads);

    public static IReadOnlyList<string> MissingIndexColumns(string showplanXml)
        => XDocument.Parse(showplanXml)
            .Descendants(Ns + "MissingIndex")
            .Descendants(Ns + "Column")
            .Select(column => Unbracket((string?)column.Attribute("Name")))
            .Where(name => name is not null)
            .Select(name => name!)
            .ToArray();

    private static IEnumerable<XElement> Counters(XElement relOp)
        => relOp.Elements(Ns + "RunTimeInformation").Elements(Ns + "RunTimeCountersPerThread");

    // An Object under a nested RelOp belongs to that one, not to this one.
    private static string? IndexOf(XElement relOp)
        => Unbracket((string?)relOp
            .Descendants(Ns + "Object")
            .FirstOrDefault(o => o.Ancestors(Ns + "RelOp").First() == relOp)
            ?.Attribute("Index"));

    private static string? Unbracket(string? name) => name?.Trim('[', ']');
}
