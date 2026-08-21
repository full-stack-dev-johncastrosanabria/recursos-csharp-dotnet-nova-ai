using Shouldly;

namespace Training.Audit.Tests;

public sealed class StubLeakCheckerTests
{
    private static TrxTest Test(string className, string method, string outcome)
        => new(className, method, outcome, "/repo/modules/01-demo/tests/UnitTests/bin/x.dll");

    [Fact]
    public void Accepts_a_class_with_at_least_one_failure()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
            Test("Training.Module01.Tests.Core.MoneyTests", "Rejects", "Failed"),
        ]);

        StubLeakChecker.Run(report).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_class_where_every_test_passed_against_the_stubs()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module14.Tests.Core.MediatorTests", "Dispatches", "Passed"),
            Test("Training.Module14.Tests.Core.MediatorTests", "Orders", "Passed"),
        ]);

        var findings = StubLeakChecker.Run(report);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("already contains the answer");
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

        StubLeakChecker.Run(report).Count.ShouldBe(1);
    }

    [Fact]
    public void Ignores_tests_outside_the_modules_folder()
    {
        var report = TrxReport.FromTests(
        [
            new TrxTest("Training.Audit.Tests.PairCheckerTests", "Works", "Passed", "/repo/tools/x.dll"),
        ]);

        StubLeakChecker.Run(report).ShouldBeEmpty();
    }
}
