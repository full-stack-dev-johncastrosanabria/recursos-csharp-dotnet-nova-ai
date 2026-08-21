using Shouldly;

namespace Training.Audit.Tests;

public sealed class StatusReporterTests
{
    private static TrxTest InModule(string module, string method, string outcome)
        => new($"Training.Tests.{method}Tests", method, outcome,
               $"/repo/modules/{module}/tests/UnitTests/bin/Debug/net10.0/x.dll");

    [Fact]
    public void Groups_results_by_module_in_numeric_order()
    {
        var report = TrxReport.FromTests(
        [
            InModule("03-async-await-and-the-thread-pool", "Deadlock", "Failed"),
            InModule("01-type-system-and-memory", "Money", "Passed"),
        ]);

        var output = StatusReporter.Render(report);
        var moduleOne = output.IndexOf("01-type-system-and-memory", StringComparison.Ordinal);
        var moduleThree = output.IndexOf("03-async-await-and-the-thread-pool", StringComparison.Ordinal);

        moduleOne.ShouldBeLessThan(moduleThree);
    }

    [Fact]
    public void Shows_solved_over_total_for_each_module()
    {
        var report = TrxReport.FromTests(
        [
            InModule("01-type-system-and-memory", "Money", "Passed"),
            InModule("01-type-system-and-memory", "Basket", "Passed"),
            InModule("01-type-system-and-memory", "Boxing", "Failed"),
        ]);

        StatusReporter.Render(report).ShouldContain("2/3");
    }

    [Fact]
    public void Marks_a_fully_solved_module_as_done()
    {
        var report = TrxReport.FromTests([InModule("01-type-system-and-memory", "Money", "Passed")]);

        StatusReporter.Render(report).ShouldContain("done");
    }

    [Fact]
    public void Reports_an_empty_run_without_throwing()
    {
        StatusReporter.Render(TrxReport.FromTests([])).ShouldContain("No module tests");
    }
}
