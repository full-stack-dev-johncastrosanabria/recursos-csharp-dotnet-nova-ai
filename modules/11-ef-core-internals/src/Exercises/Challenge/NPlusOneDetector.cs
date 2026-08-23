namespace Training.Module11.Challenge;

/// <summary>A query shape that ran far more often than it should have.</summary>
public sealed record NPlusOneFinding(string Shape, int Repetitions);

/// <summary>
/// Challenge: find the N+1 in a command log.
///
/// An N+1 is invisible in code review -- the loop looks like a loop and the
/// property access looks like a property access -- and it is unmistakable in a
/// log, because the same query shape appears over and over with only the
/// parameter changing. That is the signal, and it is worth being able to detect
/// mechanically: 200 near-identical statements is not something you spot by
/// scrolling.
///
/// Normalise reduces a statement to its shape: whitespace collapsed to single
/// spaces and trimmed, then every single-quoted literal, every bare run of
/// digits, and every parameter placeholder (an @ followed by letters, digits or
/// underscores) replaced with a single question mark.
///
/// Detect normalises every command, groups them, and returns the shape that
/// occurs most often together with its count -- but only when that count
/// reaches the threshold. Below it, return null: a query running twice is a
/// query running twice, not a finding.
/// </summary>
public static class NPlusOneDetector
{
    public static string Normalise(string sql) => throw new NotImplementedException();

    public static NPlusOneFinding? Detect(IReadOnlyList<string> commands, int threshold)
        => throw new NotImplementedException();
}
