namespace Training.Audit;

/// <summary>
/// Every guide has the same eight sections in the same order, and 3,000–5,000
/// prose words. Under 3,000 means the module is incomplete: the author stopped
/// before the material did.
/// </summary>
public static class GuideAnatomyChecker
{
    public const string Name = "guides";
    public const int MinimumWords = 3000;
    public const int MaximumWords = 5000;

    public static readonly string[] RequiredSections =
    [
        "Before you start",
        "Objectives",
        "Sections",
        "Real-world case",
        "Exercises",
        "Summary",
        "Review questions",
        "Resources",
    ];

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            var guidePath = Path.Combine(module, "GUIDE.md");
            var relative = Path.GetRelativePath(repoRoot, guidePath).Replace('\\', '/');

            if (!File.Exists(guidePath))
            {
                findings.Add(new AuditFinding(Name, relative, "is missing."));
                continue;
            }

            var markdown = File.ReadAllText(guidePath);
            var headings = GuideText.SectionHeadings(markdown);
            var position = 0;

            foreach (var required in RequiredSections)
            {
                var index = headings.ToList().IndexOf(required);

                if (index < 0)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"is missing the required section '{required}'."));
                }
                else if (index < position)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"has '{required}' out of order. The anatomy is fixed: "
                        + string.Join(" → ", RequiredSections)));
                }
                else
                {
                    position = index;
                }
            }

            var words = GuideText.CountProseWords(markdown);
            if (words < MinimumWords || words > MaximumWords)
            {
                findings.Add(new AuditFinding(
                    Name, relative,
                    $"has {words} prose words, outside the required {MinimumWords}–{MaximumWords}. "
                    + (words < MinimumWords
                        ? "Go back to the source and find what is missing."
                        : "Split the material or cut the padding.")));
            }
        }

        return findings;
    }
}
