using Shouldly;

namespace Training.Audit.Tests;

public sealed class TrxReportTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.trx");

    private const string Sample = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testId="a" testName="Training.Module01.Tests.Core.MoneyTests.Adds" outcome="Failed" />
            <UnitTestResult testId="b" testName="Training.Module01.Tests.Core.MoneyTests.Rejects" outcome="Passed" />
          </Results>
          <TestDefinitions>
            <UnitTest id="a" name="Adds">
              <TestMethod codeBase="/repo/modules/01-demo/tests/UnitTests/bin/Module01.UnitTests.dll"
                          className="Training.Module01.Tests.Core.MoneyTests" name="Adds" />
            </UnitTest>
            <UnitTest id="b" name="Rejects">
              <TestMethod codeBase="/repo/modules/01-demo/tests/UnitTests/bin/Module01.UnitTests.dll"
                          className="Training.Module01.Tests.Core.MoneyTests" name="Rejects" />
            </UnitTest>
          </TestDefinitions>
        </TestRun>
        """;

    [Fact]
    public void Reads_every_result_with_its_class_and_outcome()
    {
        File.WriteAllText(_file, Sample);

        var report = TrxReport.Load(_file);

        report.Tests.Count.ShouldBe(2);
        report.Tests.ShouldAllBe(t => t.ClassName == "Training.Module01.Tests.Core.MoneyTests");
        report.Tests.Count(t => t.Failed).ShouldBe(1);
    }

    [Fact]
    public void Reads_the_code_base_so_results_can_be_traced_to_a_module()
    {
        File.WriteAllText(_file, Sample);

        TrxReport.Load(_file).Tests[0].CodeBase.ShouldContain("modules/01-demo");
    }

    [Fact]
    public void Merges_every_report_in_a_directory()
    {
        // dotnet test runs one project at a time, so a full run leaves one
        // .trx per module rather than one for the run.
        var directory = Directory.CreateTempSubdirectory("trx-dir").FullName;
        File.WriteAllText(Path.Combine(directory, "01.trx"), Sample);
        File.WriteAllText(Path.Combine(directory, "02.trx"), Sample);

        try
        {
            TrxReport.Load(directory).Tests.Count.ShouldBe(4);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Falls_back_to_the_test_name_when_no_definition_matches_the_id()
    {
        // TestDefinitions can omit a UnitTest for a given testId (or the id
        // just doesn't match). The class name and method are then derived
        // from testName, and the codeBase - which only TestMethod carries -
        // is empty rather than thrown from.
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="missing" testName="Training.Module02.Tests.Core.BasketTests.Adds" outcome="Passed" />
              </Results>
              <TestDefinitions />
            </TestRun>
            """;
        File.WriteAllText(_file, xml);

        var test = TrxReport.Load(_file).Tests[0];

        test.ClassName.ShouldBe("Training.Module02.Tests.Core.BasketTests");
        test.MethodName.ShouldBe("Adds");
        test.CodeBase.ShouldBe(string.Empty);
    }

    [Fact]
    public void Reports_a_clear_error_for_a_path_that_is_neither_file_nor_directory()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.trx");

        var ex = Should.Throw<TrxReportException>(() => TrxReport.Load(missing));

        ex.Message.ShouldContain(missing);
    }

    [Fact]
    public void Reports_which_file_failed_to_parse_instead_of_reading_as_clean()
    {
        // A CI process killed mid-write is the realistic case: the file
        // exists but its XML is truncated or otherwise malformed. A gate
        // that cannot read its input must never report success.
        File.WriteAllText(_file, "this is not xml");

        var ex = Should.Throw<TrxReportException>(() => TrxReport.Load(_file));

        ex.Message.ShouldContain(_file);
    }

    [Fact]
    public void Names_the_malformed_file_when_one_report_in_a_directory_is_bad()
    {
        var directory = Directory.CreateTempSubdirectory("trx-dir-bad").FullName;
        var badFile = Path.Combine(directory, "02-bad.trx");
        File.WriteAllText(Path.Combine(directory, "01-good.trx"), Sample);
        File.WriteAllText(badFile, "this is not xml");

        try
        {
            var ex = Should.Throw<TrxReportException>(() => TrxReport.Load(directory));

            ex.Message.ShouldContain(badFile);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_file))
        {
            File.Delete(_file);
        }
    }
}
