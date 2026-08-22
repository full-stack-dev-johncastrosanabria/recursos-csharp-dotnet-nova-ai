namespace Training.Audit;

/// <summary>
/// Run without -p:UseSolutions=true, every test in an exercise test class
/// must fail. A single passing test is exactly as much of a leak as a
/// wholly green class: either the stub in src/Exercises already carries the
/// answer, or the test never touches the stub at all (see CONTRIBUTING.md's
/// note on writing exercise tests).
///
/// A TRX with zero results for a module is not evidence that module is
/// clean — it can just as easily mean the test host crashed or the project
/// was renamed. So this also checks the repo root: every module on disk
/// that has test files must be represented in the report, or the run is
/// named as untrustworthy rather than read as "nothing leaked".
/// </summary>
public static class StubLeakChecker
{
    public const string Name = "stub-leak";

    public static IReadOnlyList<AuditFinding> Run(TrxReport report, string repoRoot)
    {
        var findings = new List<AuditFinding>();

        var exerciseTests = report.Tests
            .Where(t => RepoLayout.ModuleNameFromPath(t.CodeBase) is not null)
            .GroupBy(t => t.ClassName, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var testClass in exerciseTests)
        {
            if (testClass.All(t => t.Failed))
            {
                continue;
            }

            var failed = testClass.Count(t => t.Failed);
            var total = testClass.Count();

            findings.Add(new AuditFinding(
                Name,
                testClass.Key,
                $"only {failed} of {total} test(s) failed against the stubs in src/Exercises. Every "
                + "test in an exercise class must fail before it is implemented: either the stub "
                + "already contains a working answer and should throw NotImplementedException "
                + "instead, or a passing test never calls the stub (for example, an assertion that "
                + "only inspects type metadata) and must be rewritten to exercise a stub member — "
                + "see CONTRIBUTING.md."));
        }

        var modulesInReport = report.Tests
            .Select(t => RepoLayout.ModuleNameFromPath(t.CodeBase))
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            var testFileCount = RepoLayout.TestFiles(module).Count();
            if (testFileCount == 0)
            {
                // Nothing has been written for this module yet — there is
                // nothing it could have contributed to the report.
                continue;
            }

            var moduleName = Path.GetFileName(module)!;
            if (modulesInReport.Contains(moduleName))
            {
                continue;
            }

            findings.Add(new AuditFinding(
                Name,
                moduleName,
                $"has {testFileCount} test file(s) on disk but contributed no results to this TRX "
                + "run. An empty result is not evidence the stubs are clean — it can mean the test "
                + "host crashed, the project was renamed, or nothing ran at all. Fix the run so this "
                + "module reports before trusting stub-leak's output."));
        }

        return findings;
    }
}
