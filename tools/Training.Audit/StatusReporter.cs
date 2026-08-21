using System.Text;

namespace Training.Audit;

/// <summary>
/// A per-module progress table, derived from a normal (non-solutions) test run.
/// Failing means unsolved, which is the correct starting state.
///
/// Thirty modules is long enough that people lose their place, and a visible
/// map of what is done is the difference between a path someone returns to and
/// a repo someone abandons in tier 3.
/// </summary>
public static class StatusReporter
{
    public static string Render(TrxReport report)
    {
        var byModule = report.Tests
            .Select(t => (Module: RepoLayout.ModuleNameFromPath(t.CodeBase), Test: t))
            .Where(x => x.Module is not null)
            .GroupBy(x => x.Module!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        if (byModule.Count == 0)
        {
            return "No module tests in this run.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("module                                    solved   state");
        builder.AppendLine("--------------------------------------------------------");

        foreach (var module in byModule)
        {
            var total = module.Count();
            var solved = module.Count(x => !x.Test.Failed);
            var state = solved == total ? "done" : solved == 0 ? "not started" : "in progress";

            builder.AppendLine($"{module.Key,-40}  {solved}/{total,-5}  {state}");
        }

        var allTests = byModule.Sum(m => m.Count());
        var allSolved = byModule.Sum(m => m.Count(x => !x.Test.Failed));
        builder.AppendLine("--------------------------------------------------------");
        builder.AppendLine($"{"total",-40}  {allSolved}/{allTests}");

        return builder.ToString();
    }
}
