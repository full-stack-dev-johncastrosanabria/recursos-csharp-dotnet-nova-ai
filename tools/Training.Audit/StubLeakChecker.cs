namespace Training.Audit;

/// <summary>
/// Run without -p:UseSolutions=true, every exercise test class must contain at
/// least one failure. A class that is entirely green means its stub in
/// src/Exercises already carries the answer.
/// </summary>
public static class StubLeakChecker
{
    public const string Name = "stub-leak";

    public static IReadOnlyList<AuditFinding> Run(TrxReport report)
    {
        var findings = new List<AuditFinding>();

        var exerciseTests = report.Tests
            .Where(t => RepoLayout.ModuleNameFromPath(t.CodeBase) is not null)
            .GroupBy(t => t.ClassName, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var testClass in exerciseTests)
        {
            if (testClass.Any(t => t.Failed))
            {
                continue;
            }

            findings.Add(new AuditFinding(
                Name,
                testClass.Key,
                $"passed entirely without -p:UseSolutions=true, across {testClass.Count()} test(s). "
                + "That means the stub in src/Exercises already contains the answer."));
        }

        return findings;
    }
}
