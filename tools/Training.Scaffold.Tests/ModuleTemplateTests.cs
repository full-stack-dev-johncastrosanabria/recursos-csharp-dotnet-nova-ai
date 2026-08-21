using Shouldly;
using Training.Audit;

namespace Training.Scaffold.Tests;

public sealed class ModuleTemplateTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("scaffold-tests").FullName;

    private string Path(params string[] parts)
        => System.IO.Path.Combine([_root, "modules", "07-the-middleware-pipeline", .. parts]);

    public ModuleTemplateTests()
        => ModuleTemplate.Create(_root, "07-the-middleware-pipeline", "The middleware pipeline", 7);

    [Fact]
    public void Creates_the_three_projects()
    {
        File.Exists(Path("src", "Exercises", "Exercises.csproj")).ShouldBeTrue();
        File.Exists(Path("src", "Solutions", "Solutions.csproj")).ShouldBeTrue();
        File.Exists(Path("tests", "UnitTests", "UnitTests.csproj")).ShouldBeTrue();
    }

    [Fact]
    public void Marks_the_test_project_so_the_swap_target_applies()
    {
        File.ReadAllText(Path("tests", "UnitTests", "UnitTests.csproj"))
            .ShouldContain("<IsTrainingTestProject>true</IsTrainingTestProject>");
    }

    [Fact]
    public void Gives_both_source_projects_the_same_root_namespace()
    {
        const string expected = "<RootNamespace>Training.Module07</RootNamespace>";

        File.ReadAllText(Path("src", "Exercises", "Exercises.csproj")).ShouldContain(expected);
        File.ReadAllText(Path("src", "Solutions", "Solutions.csproj")).ShouldContain(expected);
    }

    [Fact]
    public void Writes_the_scoped_analyser_relaxation_only_under_Exercises()
    {
        var relaxation = File.ReadAllText(Path("src", "Exercises", ".editorconfig"));

        relaxation.ShouldContain("IDE0060");
        relaxation.ShouldContain("CA1801");
        relaxation.ShouldContain("CA1822");
        relaxation.ShouldContain("CS1998");
        File.Exists(Path("src", "Solutions", ".editorconfig")).ShouldBeFalse();
    }

    [Fact]
    public void Writes_a_guide_skeleton_with_every_required_section_in_order()
    {
        var headings = GuideText.SectionHeadings(File.ReadAllText(Path("GUIDE.md")));

        headings.ShouldBe(GuideAnatomyChecker.RequiredSections);
    }

    [Fact]
    public void Creates_the_Core_and_Challenge_folders_in_both_source_projects()
    {
        Directory.Exists(Path("src", "Exercises", "Core")).ShouldBeTrue();
        Directory.Exists(Path("src", "Exercises", "Challenge")).ShouldBeTrue();
        Directory.Exists(Path("src", "Solutions", "Core")).ShouldBeTrue();
        Directory.Exists(Path("src", "Solutions", "Challenge")).ShouldBeTrue();
    }

    [Fact]
    public void Refuses_to_overwrite_an_already_scaffolded_module()
    {
        var guidePath = Path("GUIDE.md");
        const string sentinel = "SENTINEL: hand-authored prose that must survive a re-run.";
        File.WriteAllText(guidePath, sentinel);

        Should.Throw<ModuleTemplateException>(() =>
            ModuleTemplate.Create(_root, "07-the-middleware-pipeline", "The middleware pipeline", 7));

        // The property that matters: the author's prose is untouched, not just that it threw.
        File.ReadAllText(guidePath).ShouldBe(sentinel);
    }

    [Fact]
    public void Refuses_a_number_that_does_not_match_the_slugs_own_leading_digits()
    {
        var otherRoot = System.IO.Path.Combine(_root, "mismatch-check");

        Should.Throw<ModuleTemplateException>(() =>
            ModuleTemplate.Create(otherRoot, "07-the-middleware-pipeline", "The middleware pipeline", 3));
    }

    [Fact]
    public void TryParseModuleNumber_parses_the_leading_two_digit_number()
    {
        ModuleTemplate.TryParseModuleNumber("07-the-middleware-pipeline", out var number).ShouldBeTrue();
        number.ShouldBe(7);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("7-x")]
    [InlineData("7x-y")]
    [InlineData("")]
    public void TryParseModuleNumber_rejects_a_slug_without_two_digits_and_a_hyphen(string slug)
    {
        ModuleTemplate.TryParseModuleNumber(slug, out _).ShouldBeFalse();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
