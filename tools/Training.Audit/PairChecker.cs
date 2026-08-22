namespace Training.Audit;

/// <summary>
/// Every test file must have a stub in src/Exercises AND a reference
/// implementation in src/Solutions.
///
/// The solvability job already proves each solution satisfies its test. This
/// proves the other half: that the learner's stub exists at all. Without it, a
/// pull request that adds a test and a solution but forgets the stub goes green.
/// </summary>
public static class PairChecker
{
    public const string Name = "pairs";

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            foreach (var testFile in RepoLayout.TestFiles(module))
            {
                foreach (var project in (string[])["Exercises", "Solutions"])
                {
                    var counterpart = project == "Exercises"
                        ? RepoLayout.ExerciseCounterpart(module, testFile)
                        : RepoLayout.SolutionCounterpart(module, testFile);

                    if (counterpart is null || File.Exists(counterpart))
                    {
                        continue;
                    }

                    findings.Add(new AuditFinding(
                        Name,
                        Relative(repoRoot, testFile),
                        $"has no counterpart at {Relative(repoRoot, counterpart)}. "
                        + "Every test needs both a stub and a reference implementation."));
                }
            }
        }

        return findings;
    }

    private static string Relative(string repoRoot, string path)
        => Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
}
