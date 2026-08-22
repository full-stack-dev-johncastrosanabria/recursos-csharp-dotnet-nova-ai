using System.Xml;
using System.Xml.Linq;

namespace Training.Audit;

public sealed record TrxTest(string ClassName, string MethodName, string Outcome, string CodeBase)
{
    public bool Failed => string.Equals(Outcome, "Failed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True only for a genuine pass. Deliberately not "!Failed" — a skipped
    /// or errored test is neither a failure nor solved, and counting it as
    /// solved would let an exercise the learner never actually ran show up
    /// as done.
    /// </summary>
    public bool Passed => string.Equals(Outcome, "Passed", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A TRX path that could not be turned into results: the path named neither a
/// file nor a directory, or a .trx file exists but could not be read/parsed.
/// A gate that cannot read its input must never report success, so this is a
/// usage error (exit 2 in the CLI), not a swallowed/skipped file.
/// </summary>
public sealed class TrxReportException : Exception
{
    public TrxReportException(string message) : base(message)
    {
    }

    public TrxReportException(string message, Exception inner) : base(message, inner)
    {
    }
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
            // A foreach (rather than SelectMany) so a bad file's name is
            // known at the point of failure, not lost behind deferred LINQ.
            var merged = new List<TrxTest>();
            foreach (var file in Directory
                         .EnumerateFiles(path, "*.trx", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                merged.AddRange(LoadFile(file).Tests);
            }

            return new TrxReport(merged);
        }

        if (File.Exists(path))
        {
            return LoadFile(path);
        }

        throw new TrxReportException(
            $"TRX path not found: '{path}'. Pass a .trx file or a directory containing .trx files.");
    }

    private static TrxReport LoadFile(string path)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            // Realistic cause: a CI process killed mid-write leaves a
            // truncated file. Name it explicitly rather than skipping it
            // silently - a gate that cannot read its input must not report
            // success.
            throw new TrxReportException($"Could not read TRX file '{path}': {ex.Message}", ex);
        }

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
