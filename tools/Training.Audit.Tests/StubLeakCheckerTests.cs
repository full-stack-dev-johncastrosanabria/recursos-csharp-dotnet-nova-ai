using Shouldly;

namespace Training.Audit.Tests;

public sealed class StubLeakCheckerTests : IDisposable
{
    // A repo root with no modules/ folder at all: RepoLayout.ModuleDirectories
    // returns nothing for it, so the "every module on disk contributed
    // results" half of the checker never fires. Tests aimed only at the
    // per-class leak rule use this so they exercise exactly one behaviour.
    private readonly string _noModulesRoot = Directory.CreateTempSubdirectory("stub-leak-tests").FullName;

    private static TrxTest Test(string className, string method, string outcome)
        => new(className, method, outcome, "/repo/modules/01-demo/tests/UnitTests/bin/x.dll");

    [Fact]
    public void Accepts_a_class_where_every_test_failed()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
            Test("Training.Module01.Tests.Core.MoneyTests", "Rejects", "Failed"),
        ]);

        StubLeakChecker.Run(report, _noModulesRoot).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_class_where_every_test_passed_against_the_stubs()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module14.Tests.Core.MediatorTests", "Dispatches", "Passed"),
            Test("Training.Module14.Tests.Core.MediatorTests", "Orders", "Passed"),
        ]);

        var findings = StubLeakChecker.Run(report, _noModulesRoot);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("NotImplementedException");
    }

    [Fact]
    public void Reports_a_class_where_only_some_tests_failed_against_the_stubs()
    {
        // The exact hole this fix closes: Add is implemented, Subtract still
        // throws. Two of three tests come back green. The old rule — "at
        // least one failure" — read this as clean.
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.CalculatorTests", "Add", "Passed"),
            Test("Training.Module01.Tests.Core.CalculatorTests", "AddNegatives", "Passed"),
            Test("Training.Module01.Tests.Core.CalculatorTests", "Subtract", "Failed"),
        ]);

        var findings = StubLeakChecker.Run(report, _noModulesRoot);

        findings.Count.ShouldBe(1);
        findings[0].Path.ShouldBe("Training.Module01.Tests.Core.CalculatorTests");
        findings[0].Message.ShouldContain("1 of 3");
    }

    [Fact]
    public void Names_both_possible_causes_of_a_partial_leak()
    {
        // A legitimate test can pass against an untouched stub if it never
        // calls it (e.g. asserting a type is a value type). The message must
        // tell the author what to do about either cause, not just the one
        // that happens to be true.
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "IsAValueType", "Passed"),
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
        ]);

        var message = StubLeakChecker.Run(report, _noModulesRoot).Single().Message;

        message.ShouldContain("NotImplementedException");
        message.ShouldContain("never calls the stub");
    }

    [Fact]
    public void Catches_a_partial_leak_that_an_aggregate_check_would_miss()
    {
        // Module 14 leaked; module 01 is still red. A whole-run assertion would
        // see "the suite failed" and report success at catching nothing.
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
            Test("Training.Module14.Tests.Core.MediatorTests", "Dispatches", "Passed"),
        ]);

        StubLeakChecker.Run(report, _noModulesRoot).Count.ShouldBe(1);
    }

    [Fact]
    public void Ignores_tests_outside_the_modules_folder()
    {
        var report = TrxReport.FromTests(
        [
            new TrxTest("Training.Audit.Tests.PairCheckerTests", "Works", "Passed", "/repo/tools/x.dll"),
        ]);

        StubLeakChecker.Run(report, _noModulesRoot).ShouldBeEmpty();
    }

    // --- module coverage (Fix 2): a TRX with nothing for a module must not read as clean ---

    private static void WriteTestFile(string root, string relativePath)
    {
        var full = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "");
    }

    [Fact]
    public void Reports_a_module_on_disk_that_contributed_no_results_to_the_trx()
    {
        var root = Directory.CreateTempSubdirectory("stub-leak-coverage-tests").FullName;
        try
        {
            WriteTestFile(root, "modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");

            // An empty run: the TRX exists but carries zero results, as it
            // would after `dotnet test ... || true` swallowed a crash.
            var findings = StubLeakChecker.Run(TrxReport.FromTests([]), root);

            findings.Count.ShouldBe(1);
            findings[0].Path.ShouldBe("01-demo");
            findings[0].Message.ShouldContain("contributed no results");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Accepts_a_run_where_every_module_on_disk_is_represented()
    {
        var root = Directory.CreateTempSubdirectory("stub-leak-coverage-tests").FullName;
        try
        {
            WriteTestFile(root, "modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");

            var report = TrxReport.FromTests(
            [
                new TrxTest(
                    "Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed",
                    "/repo/modules/01-demo/tests/UnitTests/bin/x.dll"),
            ]);

            StubLeakChecker.Run(report, root).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Does_not_require_results_from_a_module_with_no_test_files_yet()
    {
        var root = Directory.CreateTempSubdirectory("stub-leak-coverage-tests").FullName;
        try
        {
            // Freshly scaffolded, nothing authored: this must not be
            // mistaken for a run that silently dropped a module's results.
            Directory.CreateDirectory(Path.Combine(root, "modules", "02-fresh", "tests", "UnitTests"));

            StubLeakChecker.Run(TrxReport.FromTests([]), root).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public void Dispose() => Directory.Delete(_noModulesRoot, recursive: true);
}
