using Training.Audit;

namespace Training.Scaffold;

/// <summary>
/// Generates one module's three projects plus a guide skeleton.
///
/// It takes the required section list from GuideAnatomyChecker rather than
/// repeating it, so a scaffolded module can never start life failing the audit.
/// </summary>
public static class ModuleTemplate
{
    public static void Create(string repoRoot, string slug, string title, int number)
    {
        var module = Path.Combine(repoRoot, "modules", slug);
        var rootNamespace = $"Training.Module{number:D2}";

        foreach (var folder in (string[])["Core", "Challenge"])
        {
            Directory.CreateDirectory(Path.Combine(module, "src", "Exercises", folder));
            Directory.CreateDirectory(Path.Combine(module, "src", "Solutions", folder));
            Directory.CreateDirectory(Path.Combine(module, "tests", "UnitTests", folder));
        }

        Directory.CreateDirectory(Path.Combine(module, "examples"));

        Write(Path.Combine(module, "src", "Exercises", "Exercises.csproj"),
            SourceProject(rootNamespace, "Exercises"));
        Write(Path.Combine(module, "src", "Solutions", "Solutions.csproj"),
            SourceProject(rootNamespace, "Solutions"));
        Write(Path.Combine(module, "src", "Exercises", ".editorconfig"), StubRelaxation);
        Write(Path.Combine(module, "tests", "UnitTests", "UnitTests.csproj"),
            TestProject(rootNamespace, number));
        Write(RepoLayout.GuidePath(module), Guide(title, number));
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string SourceProject(string rootNamespace, string assemblyName) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{rootNamespace}</RootNamespace>
            <AssemblyName>{assemblyName}</AssemblyName>
          </PropertyGroup>
        </Project>

        """;

    private static string TestProject(string rootNamespace, int number) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{rootNamespace}.Tests</RootNamespace>
            <AssemblyName>Module{number:D2}.UnitTests</AssemblyName>
            <OutputType>Exe</OutputType>
            <IsTrainingTestProject>true</IsTrainingTestProject>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="xunit.v3" />
            <PackageReference Include="Shouldly" />
            <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
          </ItemGroup>

          <ItemGroup>
            <Using Include="Xunit" />
          </ItemGroup>
        </Project>

        """;

    /// <summary>
    /// The only analyser relaxation in the repository, scoped to stubs.
    ///
    /// Every rule here fires solely because the stub body is
    /// `throw new NotImplementedException()`: an unused parameter, a method
    /// that never touches `this`, an `async` method with no `await`. Each one
    /// starts applying again on its own the moment the learner writes a real
    /// implementation, because the implementation stops matching the reason
    /// the rule was silenced. Solutions, examples, tools and system
    /// checkpoints are held to the full ruleset — this file is generated only
    /// under src/Exercises, never under src/Solutions.
    /// </summary>
    private const string StubRelaxation =
        """
        # Scoped relaxation for exercise stubs ONLY. Do not copy this elsewhere.
        #
        # Each rule below fires purely because the method body is
        # `throw new NotImplementedException()`. Once the learner implements the
        # method, the rule applies again on its own. Solutions, examples, tools
        # and system checkpoints are held to the full ruleset.

        [*.cs]
        dotnet_diagnostic.IDE0060.severity = none   # unused parameter: nothing consumes it yet
        dotnet_diagnostic.CA1801.severity = none    # unused parameter, from the analyzer package
        dotnet_diagnostic.CA1822.severity = none    # could be static: never touches `this` yet
        dotnet_diagnostic.CS1998.severity = none    # async without await: the body only throws

        """;

    private static string Guide(string title, int number)
    {
        var sections = string.Join(
            "\n\n",
            GuideAnatomyChecker.RequiredSections.Select(section => $"## {section}"));

        return $"# Module {number:D2} · {title}\n\n{sections}\n";
    }
}
