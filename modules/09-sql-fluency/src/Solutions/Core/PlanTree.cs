using System.Text.Json;

namespace Training.Module09.Core;

/// <summary>One node of a PostgreSQL execution plan.</summary>
public sealed class PlanNode
{
    public required string NodeType { get; init; }

    public string? RelationName { get; init; }

    public string? IndexName { get; init; }

    public double PlanRows { get; init; }

    public double ActualRows { get; init; }

    public int ActualLoops { get; init; }

    public double RowsRemovedByFilter { get; init; }

    public double ActualTotalTimeMs { get; init; }

    public IReadOnlyList<PlanNode> Children { get; init; } = [];
}

/// <summary>Reading a plan: EXPLAIN (FORMAT JSON) into a tree.</summary>
public static class PlanTree
{
    public static PlanNode Parse(string explainJson)
    {
        using var document = JsonDocument.Parse(explainJson);

        // One entry per statement; EXPLAIN of a single query returns one.
        var root = document.RootElement[0].GetProperty("Plan");

        return Read(root);
    }

    public static IEnumerable<PlanNode> Walk(PlanNode root)
    {
        yield return root;

        foreach (var child in root.Children)
        {
            foreach (var descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    private static PlanNode Read(JsonElement element)
    {
        var children = element.TryGetProperty("Plans", out var plans)
            ? plans.EnumerateArray().Select(Read).ToArray()
            : [];

        return new PlanNode
        {
            NodeType = element.GetProperty("Node Type").GetString()!,
            RelationName = Text(element, "Relation Name"),
            IndexName = Text(element, "Index Name"),
            PlanRows = Number(element, "Plan Rows"),
            ActualRows = Number(element, "Actual Rows"),
            ActualLoops = (int)Number(element, "Actual Loops"),
            RowsRemovedByFilter = Number(element, "Rows Removed by Filter"),
            ActualTotalTimeMs = Number(element, "Actual Total Time"),
            Children = children,
        };
    }

    private static string? Text(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static double Number(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetDouble() : 0;
}
