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

    private static string GuideWith(int proseWords, IEnumerable<string>? sections = null)
    {
        var used = sections ?? GuideAnatomyChecker.RequiredSections;
        var body = string.Join("\n\n", used.Select(s => $"## {s}"));
        return "# Module 01\n\n" + body + "\n\n" + string.Join(' ', Enumerable.Repeat("word", proseWords));
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

        findings.ShouldNotBeEmpty();
        findings[0].Message.ShouldContain("1500");
        findings[0].Message.ShouldContain("3000");
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

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
