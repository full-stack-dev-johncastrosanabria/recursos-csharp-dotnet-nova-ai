using Shouldly;

namespace Training.Audit.Tests;

public sealed class GuideAnatomyCheckerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("guide-tests").FullName;

    private void WriteGuide(string body)
    {
        var dir = Path.Combine(_root, "modules", "01-demo");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "GUIDE.md"), body);
    }

    // Spreads proseWords evenly across every section so each one clears the
    // per-section floor on its own — a guide "with every section" has to
    // mean real content in each one, not all of it dumped under the last
    // heading.
    private static string GuideWith(int proseWords, IEnumerable<string>? sections = null)
    {
        var used = (sections ?? GuideAnatomyChecker.RequiredSections).ToList();
        var perSection = proseWords / used.Count;
        var remainder = proseWords - (perSection * used.Count);

        var body = string.Join("\n\n", used.Select((section, i) =>
        {
            var count = perSection + (i == 0 ? remainder : 0);
            return $"## {section}\n\n" + string.Join(' ', Enumerable.Repeat("word", count));
        }));

        return "# Module 01\n\n" + body + "\n";
    }

    private static string GuideWithBareSectionsExcept(string sectionWithProse, int proseWords)
    {
        var body = string.Join("\n\n", GuideAnatomyChecker.RequiredSections.Select(section =>
            section == sectionWithProse
                ? $"## {section}\n\n" + string.Join(' ', Enumerable.Repeat("word", proseWords))
                : $"## {section}"));

        return "# Module 01\n\n" + body + "\n";
    }

    [Fact]
    public void Accepts_a_guide_with_every_section_and_a_valid_word_count()
    {
        WriteGuide(GuideWith(3500));

        GuideAnatomyChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Rejects_a_guide_that_is_too_short()
    {
        WriteGuide(GuideWith(1500));

        var findings = GuideAnatomyChecker.Run(_root);

        findings.ShouldContain(f => f.Message.Contains("1500") && f.Message.Contains("3000"));
    }

    [Fact]
    public void Rejects_a_guide_that_is_too_long()
    {
        WriteGuide(GuideWith(5200));

        GuideAnatomyChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Rejects_a_missing_section()
    {
        WriteGuide(GuideWith(3500, GuideAnatomyChecker.RequiredSections.Where(s => s != "Exercises")));

        var findings = GuideAnatomyChecker.Run(_root);

        findings.ShouldNotBeEmpty();
        findings.ShouldContain(f => f.Message.Contains("Exercises"));
    }

    [Fact]
    public void Rejects_sections_that_are_out_of_order()
    {
        var reordered = GuideAnatomyChecker.RequiredSections.Reverse();
        WriteGuide(GuideWith(3500, reordered));

        GuideAnatomyChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Reports_a_misplaced_heading_once_instead_of_cascading()
    {
        // "Before you start" moved to the very end: everything else keeps
        // its correct relative order. The old position tracker stalled after
        // the first out-of-order hit and flagged every section that followed
        // it too. Only one section is actually misplaced.
        var reordered = GuideAnatomyChecker.RequiredSections
            .Where(s => s != "Before you start")
            .Append("Before you start");
        WriteGuide(GuideWith(3500, reordered));

        var outOfOrder = GuideAnatomyChecker.Run(_root)
            .Where(f => f.Message.Contains("out of order"))
            .ToList();

        outOfOrder.Count.ShouldBe(1);
    }

    [Fact]
    public void Rejects_a_required_section_that_is_present_but_essentially_empty()
    {
        // 3,200 words under "Before you start"; the other seven headings
        // bare. This is the exact hole the fix closes: a document total in
        // range and every heading present in order used to read as clean.
        WriteGuide(GuideWithBareSectionsExcept("Before you start", 3200));

        var findings = GuideAnatomyChecker.Run(_root);

        var bareSections = GuideAnatomyChecker.RequiredSections.Where(s => s != "Before you start");
        foreach (var bare in bareSections)
        {
            findings.ShouldContain(f => f.Message.Contains($"'{bare}'") && f.Message.Contains("floor"));
        }

        findings.ShouldNotContain(f => f.Message.Contains("'Before you start'") && f.Message.Contains("floor"));
    }

    [Fact]
    public void States_what_a_bare_section_is_for()
    {
        WriteGuide(GuideWithBareSectionsExcept("Sections", 3200));

        var findings = GuideAnatomyChecker.Run(_root);

        findings.ShouldContain(f =>
            f.Message.Contains("'Objectives'") && f.Message.Contains("what the reader can do afterwards"));
    }

    [Fact]
    public void Accepts_a_short_but_real_Objectives_section()
    {
        // Objectives legitimately runs to well under a hundred words while
        // Sections runs to thousands. The floor must not fight that shape.
        var sections = GuideAnatomyChecker.RequiredSections;
        var body = string.Join("\n\n", sections.Select(section =>
        {
            var count = section == "Objectives" ? 45 : 500;
            return $"## {section}\n\n" + string.Join(' ', Enumerable.Repeat("word", count));
        }));
        WriteGuide("# Module 01\n\n" + body + "\n");

        GuideAnatomyChecker.Run(_root).ShouldBeEmpty();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
