using Shouldly;

namespace Training.Audit.Tests;

public sealed class RepoLayoutTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("repo-layout-tests").FullName;

    private void WriteFile(string relativePath, string content = "")
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void Discovers_the_conventional_UnitTests_project()
    {
        WriteFile("modules/01-demo/tests/UnitTests/UnitTests.csproj");

        var found = RepoLayout.TestProjectDirectories(_root).ToList();

        found.ShouldHaveSingleItem();
        Path.GetFileName(found[0]).ShouldBe("UnitTests");
    }

    [Fact]
    public void Discovers_a_tier_with_a_name_it_does_not_already_know()
    {
        // The whole point: CI must not miss a tier just because it isn't
        // named UnitTests or IntegrationTests.
        WriteFile("modules/01-demo/tests/ContractTests/ContractTests.csproj");

        var found = RepoLayout.TestProjectDirectories(_root).ToList();

        found.ShouldHaveSingleItem();
        Path.GetFileName(found[0]).ShouldBe("ContractTests");
    }

    [Fact]
    public void Discovers_every_tier_across_every_module()
    {
        WriteFile("modules/01-demo/tests/UnitTests/UnitTests.csproj");
        WriteFile("modules/02-demo/tests/UnitTests/UnitTests.csproj");
        WriteFile("modules/02-demo/tests/IntegrationTests/IntegrationTests.csproj");

        var found = RepoLayout.TestProjectDirectories(_root)
            .Select(d => Path.GetRelativePath(_root, d).Replace('\\', '/'))
            .ToList();

        found.ShouldBe(
        [
            "modules/01-demo/tests/UnitTests",
            "modules/02-demo/tests/IntegrationTests",
            "modules/02-demo/tests/UnitTests",
        ]);
    }

    [Fact]
    public void Ignores_a_tests_subdirectory_that_has_no_csproj()
    {
        // Non-executable scratch content under tests/ (fixtures, a README)
        // must not be handed to `dotnet test` as though it were a project.
        WriteFile("modules/01-demo/tests/Fixtures/sample.json", "{}");

        RepoLayout.TestProjectDirectories(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Returns_nothing_when_there_are_no_modules()
    {
        RepoLayout.TestProjectDirectories(_root).ShouldBeEmpty();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
