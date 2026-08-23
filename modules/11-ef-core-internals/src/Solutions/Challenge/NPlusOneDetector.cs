using System.Text.RegularExpressions;

namespace Training.Module11.Challenge;

/// <summary>A query shape that ran far more often than it should have.</summary>
public sealed record NPlusOneFinding(string Shape, int Repetitions);

/// <summary>
/// Finding the N+1 in a command log: the same shape over and over, with only
/// the parameter changing.
/// </summary>
public static partial class NPlusOneDetector
{
    public static string Normalise(string sql)
    {
        var shape = Whitespace().Replace(sql, " ").Trim();
        shape = StringLiteral().Replace(shape, "?");
        shape = Parameter().Replace(shape, "?");
        shape = Number().Replace(shape, "?");

        return shape;
    }

    public static NPlusOneFinding? Detect(IReadOnlyList<string> commands, int threshold)
    {
        var worst = commands
            .Select(Normalise)
            .GroupBy(shape => shape, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        return worst is not null && worst.Count() >= threshold
            ? new NPlusOneFinding(worst.Key, worst.Count())
            : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("'[^']*'")]
    private static partial Regex StringLiteral();

    [GeneratedRegex(@"@[A-Za-z0-9_]+")]
    private static partial Regex Parameter();

    [GeneratedRegex(@"\b\d+\b")]
    private static partial Regex Number();
}
