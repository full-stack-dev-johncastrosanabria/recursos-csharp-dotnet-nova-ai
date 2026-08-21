namespace Training.Audit;

/// <summary>
/// The repository's path conventions, in one place. Every other check asks
/// this class where things live rather than composing paths itself.
/// </summary>
public static class RepoLayout
{
    public const string ModulesFolder = "modules";
    public const string TestSuffix = "Tests.cs";

    public static IEnumerable<string> ModuleDirectories(string repoRoot)
    {
        var modules = Path.Combine(repoRoot, ModulesFolder);
        return Directory.Exists(modules)
            ? Directory.EnumerateDirectories(modules).OrderBy(d => d, StringComparer.Ordinal)
            : [];
    }

    /// <summary>Every *Tests.cs under a module's tests/ folder.</summary>
    public static IEnumerable<string> TestFiles(string moduleDirectory)
    {
        var tests = Path.Combine(moduleDirectory, "tests");
        return Directory.Exists(tests)
            ? Directory.EnumerateFiles(tests, "*" + TestSuffix, SearchOption.AllDirectories)
                       .OrderBy(f => f, StringComparer.Ordinal)
            : [];
    }

    /// <summary>
    /// modules/01-x/tests/UnitTests/Core/MoneyTests.cs
    ///   -> modules/01-x/src/Exercises/Core/Money.cs
    /// Returns null when the path is not a recognisable test file.
    /// </summary>
    public static string? ExerciseCounterpart(string moduleDirectory, string testFilePath)
        => Counterpart(moduleDirectory, testFilePath, "Exercises");

    public static string? SolutionCounterpart(string moduleDirectory, string testFilePath)
        => Counterpart(moduleDirectory, testFilePath, "Solutions");

    private static string? Counterpart(string moduleDirectory, string testFilePath, string project)
    {
        var fileName = Path.GetFileName(testFilePath);
        if (!fileName.EndsWith(TestSuffix, StringComparison.Ordinal))
        {
            return null;
        }

        // Everything between the test project folder and the file itself is
        // preserved, so Core/ and Challenge/ map straight across.
        var testsRoot = Path.Combine(moduleDirectory, "tests");
        var relative = Path.GetRelativePath(testsRoot, testFilePath);
        var segments = relative.Split(Path.DirectorySeparatorChar);
        if (segments.Length < 2)
        {
            return null;
        }

        var subPath = Path.Combine(segments[1..^1]);
        var sourceName = fileName[..^TestSuffix.Length] + ".cs";

        return Path.Combine(moduleDirectory, "src", project, subPath, sourceName);
    }
}
