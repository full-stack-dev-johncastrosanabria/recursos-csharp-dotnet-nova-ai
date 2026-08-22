namespace Training.Audit;

/// <summary>
/// Every guide has the same eight sections in the same order, 3,000–5,000
/// prose words overall, and real content under each heading individually.
/// Under 3,000 total means the module stopped before the material did; a
/// heading with next to nothing under it means a section was left decorative
/// — present in name only, satisfying the anatomy check without carrying the
/// real-world case, exercises, or whatever else that heading exists for.
/// </summary>
public static class GuideAnatomyChecker
{
    public const string Name = "guides";
    public const int MinimumWords = 3000;
    public const int MaximumWords = 5000;

    /// <summary>
    /// The floor for a single section's prose. Deliberately low: "Objectives"
    /// or "Summary" can legitimately run under a hundred words, while
    /// "Sections" runs to thousands, so one global number has to sit well
    /// under the shortest legitimate section rather than aim at an "average"
    /// one. This does not police length — MinimumWords/MaximumWords already
    /// does that for the document as a whole — it only rules out a heading
    /// with a single filler line or nothing at all under it.
    /// </summary>
    public const int MinimumSectionWords = 40;

    public static readonly IReadOnlyList<string> RequiredSections =
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

    /// <summary>What each required section is for, used to explain a bare-section finding.</summary>
    private static readonly Dictionary<string, string> SectionPurpose = new()
    {
        ["Before you start"] = "prerequisites, realistic time, and where to skip to if you already "
            + "know the material",
        ["Objectives"] = "what the reader can do afterwards, stated as verbs",
        ["Sections"] = "the teaching content itself",
        ["Real-world case"] = "a specific, reproducible failure caused by not understanding this "
            + "module, with a derived cost",
        ["Exercises"] = "the exercises themselves, each tied to a test the reader runs",
        ["Summary"] = "recapping what the module covered",
        ["Review questions"] = "questions that check what the reader retained",
        ["Resources"] = "verified links, each with one line on why it's worth the reader's time",
    };

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            var guidePath = RepoLayout.GuidePath(module);
            var relative = Path.GetRelativePath(repoRoot, guidePath).Replace('\\', '/');

            if (!File.Exists(guidePath))
            {
                findings.Add(new AuditFinding(Name, relative, "is missing."));
                continue;
            }

            var markdown = File.ReadAllText(guidePath);
            var headings = GuideText.SectionHeadings(markdown).ToList();
            var sectionWords = GuideText.ProseWordsBySection(markdown);
            var position = 0;

            foreach (var required in RequiredSections)
            {
                var index = headings.IndexOf(required);

                if (index < 0)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"is missing the required section '{required}'."));
                    continue;
                }

                if (index < position)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"has '{required}' out of order. The anatomy is fixed: "
                        + string.Join(" → ", RequiredSections)));
                }

                // Advance regardless of the branch above: a misplaced heading
                // must be reported once, not compared against every section
                // that follows it and reported again for each.
                position = index;

                var words = sectionWords.GetValueOrDefault(required);
                if (words < MinimumSectionWords)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"section '{required}' has {words} prose word(s), under the "
                        + $"{MinimumSectionWords}-word floor. This section is for "
                        + $"{SectionPurpose[required]} — a heading with next to nothing "
                        + "under it does not satisfy that."));
                }
            }

            var totalWords = GuideText.CountProseWords(markdown);
            if (totalWords < MinimumWords || totalWords > MaximumWords)
            {
                findings.Add(new AuditFinding(
                    Name, relative,
                    $"has {totalWords} prose words, outside the required {MinimumWords}–{MaximumWords}. "
                    + (totalWords < MinimumWords
                        ? "Go back to the source and find what is missing."
                        : "Split the material or cut the padding.")));
            }
        }

        return findings;
    }
}
