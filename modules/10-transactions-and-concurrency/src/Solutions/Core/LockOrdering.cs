namespace Training.Module10.Core;

/// <summary>
/// Making deadlock impossible rather than survivable: a cycle cannot form if
/// every transaction takes its locks in the same order.
/// </summary>
public static class LockOrdering
{
    public static IReadOnlyList<string> Order(IEnumerable<string> resources)
        => resources
            .Distinct(StringComparer.Ordinal)
            .OrderBy(resource => resource, StringComparer.Ordinal)
            .ToArray();

    public static bool CouldDeadlock(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var shared = first.Intersect(second, StringComparer.Ordinal).ToArray();

        // A cycle needs two resources the sequences disagree about.
        foreach (var left in shared)
        {
            foreach (var right in shared)
            {
                if (IndexIn(first, left) < IndexIn(first, right)
                    && IndexIn(second, left) > IndexIn(second, right))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int IndexIn(IReadOnlyList<string> sequence, string resource)
    {
        for (var index = 0; index < sequence.Count; index++)
        {
            if (string.Equals(sequence[index], resource, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
