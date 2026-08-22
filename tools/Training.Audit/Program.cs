using System.Diagnostics.CodeAnalysis;
using Training.Audit;

var repoRoot = Directory.GetCurrentDirectory();
var command = args.Length > 0 ? args[0] : "all";
var trxPath = ArgumentValue(args, "--trx");

if (command == "status")
{
    if (trxPath is null)
    {
        Console.Error.WriteLine("status requires --trx <path>");
        return 2;
    }

    if (!TryLoadTrx(trxPath, out var statusReport, out var statusError))
    {
        Console.Error.WriteLine(statusError);
        return 2;
    }

    Console.WriteLine(StatusReporter.Render(statusReport));
    return 0;
}

if (command == "test-projects")
{
    foreach (var directory in RepoLayout.TestProjectDirectories(repoRoot))
    {
        Console.WriteLine(Path.GetRelativePath(repoRoot, directory).Replace(Path.DirectorySeparatorChar, '/'));
    }

    return 0;
}

IReadOnlyList<AuditFinding> findings;

switch (command)
{
    case "pairs":
        findings = PairChecker.Run(repoRoot);
        break;
    case "api":
        findings = ApiSurfaceChecker.Run(repoRoot);
        break;
    case "guides":
        findings = GuideAnatomyChecker.Run(repoRoot);
        break;
    case "stub-leak":
        if (trxPath is null)
        {
            Console.Error.WriteLine("stub-leak requires --trx <path>");
            return 2;
        }

        if (!TryLoadTrx(trxPath, out var stubLeakReport, out var stubLeakError))
        {
            Console.Error.WriteLine(stubLeakError);
            return 2;
        }

        findings = StubLeakChecker.Run(stubLeakReport, repoRoot);
        break;
    case "all":
        findings =
        [
            .. PairChecker.Run(repoRoot),
            .. ApiSurfaceChecker.Run(repoRoot),
            .. GuideAnatomyChecker.Run(repoRoot),
        ];
        break;
    default:
        Console.Error.WriteLine(
            "usage: audit [all|pairs|api|guides|test-projects] | audit stub-leak --trx <path> "
            + "| audit status --trx <path>");
        return 2;
}

foreach (var finding in findings)
{
    Console.Error.WriteLine($"[{finding.Check}] {finding.Path}: {finding.Message}");
}

Console.WriteLine(findings.Count == 0 ? "audit: clean" : $"audit: {findings.Count} finding(s)");
return findings.Count == 0 ? 0 : 1;

static string? ArgumentValue(string[] arguments, string name)
{
    var index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

// A malformed/missing TRX is a usage error (exit 2), not a crash: it must
// name the bad path/file so a learner running `status` never sees a stack
// trace, and a CI gate that cannot read its input must never read as clean.
static bool TryLoadTrx(string path, [NotNullWhen(true)] out TrxReport? report, out string? error)
{
    try
    {
        report = TrxReport.Load(path);
        error = null;
        return true;
    }
    catch (TrxReportException ex)
    {
        report = null;
        error = ex.Message;
        return false;
    }
}
