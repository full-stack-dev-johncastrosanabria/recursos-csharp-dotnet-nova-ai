using System.Xml.Linq;

namespace Training.Audit;

public sealed record TrxTest(string ClassName, string MethodName, string Outcome, string CodeBase)
{
    public bool Failed => string.Equals(Outcome, "Failed", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A parsed TRX file, produced by Microsoft.Testing.Extensions.TrxReport.
/// Element names are matched on local name so the schema namespace cannot
/// break parsing when the extension version moves.
/// </summary>
public sealed class TrxReport
{
    private TrxReport(IReadOnlyList<TrxTest> tests) => Tests = tests;

    public IReadOnlyList<TrxTest> Tests { get; }

    public static TrxReport FromTests(IReadOnlyList<TrxTest> tests) => new(tests);

    /// <summary>
    /// Loads one .trx file, or merges every .trx under a directory. `dotnet test`
    /// runs a single project at a time, so a full-repo run produces one report
    /// per module rather than one for the run.
    /// </summary>
    public static TrxReport Load(string path)
    {
        if (Directory.Exists(path))
        {
            var merged = Directory
                .EnumerateFiles(path, "*.trx", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
                .SelectMany(file => LoadFile(file).Tests)
                .ToList();

            return new TrxReport(merged);
        }

        return LoadFile(path);
    }

    private static TrxReport LoadFile(string path)
    {
        var document = XDocument.Load(path);

        var definitions = document.Descendants()
            .Where(e => e.Name.LocalName == "UnitTest")
            .Select(unitTest => new
            {
                Id = unitTest.Attribute("id")?.Value ?? string.Empty,
                Method = unitTest.Elements().FirstOrDefault(e => e.Name.LocalName == "TestMethod"),
            })
            .Where(x => x.Id.Length > 0 && x.Method is not null)
            .ToDictionary(
                x => x.Id,
                x => (
                    ClassName: x.Method!.Attribute("className")?.Value ?? string.Empty,
                    CodeBase: (x.Method.Attribute("codeBase")?.Value ?? string.Empty).Replace('\\', '/')),
                StringComparer.Ordinal);

        var tests = new List<TrxTest>();

        foreach (var result in document.Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
        {
            var id = result.Attribute("testId")?.Value ?? string.Empty;
            var outcome = result.Attribute("outcome")?.Value ?? "Unknown";
            var testName = result.Attribute("testName")?.Value ?? string.Empty;

            var found = definitions.TryGetValue(id, out var definition);
            var className = found ? definition.ClassName : ClassNameFrom(testName);
            var codeBase = found ? definition.CodeBase : string.Empty;
            var methodName = testName.Length > className.Length && className.Length > 0
                ? testName[(className.Length + 1)..]
                : testName;

            tests.Add(new TrxTest(className, methodName, outcome, codeBase));
        }

        return new TrxReport(tests);
    }

    private static string ClassNameFrom(string testName)
    {
        var lastDot = testName.LastIndexOf('.');
        return lastDot < 0 ? testName : testName[..lastDot];
    }
}
