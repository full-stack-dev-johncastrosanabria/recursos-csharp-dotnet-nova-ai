using Shouldly;

namespace Training.Audit.Tests;

public sealed class PairCheckerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("audit-tests").FullName;

    private void WriteFile(string relativePath, string content = "")
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void Reports_nothing_when_every_test_has_both_counterparts()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Exercises/Core/Money.cs");
        WriteFile("modules/01-demo/src/Solutions/Core/Money.cs");

        PairChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_the_missing_stub_when_only_the_solution_exists()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Solutions/Core/Money.cs");

        var findings = PairChecker.Run(_root);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("src/Exercises/Core/Money.cs");
    }

    [Fact]
    public void Reports_the_missing_solution_when_only_the_stub_exists()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Exercises/Core/Money.cs");

        var findings = PairChecker.Run(_root);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("src/Solutions/Core/Money.cs");
    }

    [Fact]
    public void Ignores_test_files_that_do_not_end_in_Tests()
    {
        WriteFile("modules/01-demo/tests/UnitTests/TestHelpers.cs");

        PairChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Ignores_a_file_called_exactly_Tests_cs()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/Tests.cs");

        PairChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Maps_a_test_file_in_the_project_root_to_the_source_root()
    {
        WriteFile("modules/01-demo/tests/UnitTests/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Solutions/Money.cs");

        var findings = PairChecker.Run(_root);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("src/Exercises/Money.cs");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
