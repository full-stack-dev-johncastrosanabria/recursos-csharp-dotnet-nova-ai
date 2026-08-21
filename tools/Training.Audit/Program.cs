using Training.Audit;

var repoRoot = Directory.GetCurrentDirectory();
var command = args.Length > 0 ? args[0] : "all";

IReadOnlyList<AuditFinding> findings = command switch
{
    "pairs" => PairChecker.Run(repoRoot),
    "all" => PairChecker.Run(repoRoot),
    _ => throw new ArgumentException($"Unknown command '{command}'."),
};

foreach (var finding in findings)
{
    Console.Error.WriteLine($"[{finding.Check}] {finding.Path}: {finding.Message}");
}

Console.WriteLine(findings.Count == 0
    ? "audit: clean"
    : $"audit: {findings.Count} finding(s)");

return findings.Count == 0 ? 0 : 1;
