using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class ShowplanTests
{
    [Fact]
    public void A_function_wrapped_predicate_scans_the_index_instead_of_seeking_it()
    {
        var plan = Plans.LoadXml(Plans.SqlServerFunctionWrapped);

        var scan = Showplan.Operators(plan).Single();

        scan.PhysicalOp.ShouldBe("Index Scan");
        scan.Index.ShouldBe("orders_customer_email_idx");
        Showplan.UsedSeek(plan).ShouldBeFalse();
    }

    [Fact]
    public void And_costs_a_hundred_times_the_pages_of_the_seek()
    {
        // SQL Server's version of the same contrast. Four rows either way; 341
        // pages read against 3.
        Showplan.TotalLogicalReads(Plans.LoadXml(Plans.SqlServerFunctionWrapped)).ShouldBe(341);
        Showplan.TotalLogicalReads(Plans.LoadXml(Plans.SqlServerDirect)).ShouldBe(3);
    }

    [Fact]
    public void The_sargable_form_seeks()
    {
        var plan = Plans.LoadXml(Plans.SqlServerDirect);

        Showplan.UsedSeek(plan).ShouldBeTrue();
        Showplan.Operators(plan).Single().PhysicalOp.ShouldBe("Index Seek");
    }

    [Fact]
    public void Actual_and_estimated_rows_are_both_reported()
    {
        var scan = Showplan.Operators(Plans.LoadXml(Plans.SqlServerYearFunction)).Single();

        scan.EstimateRows.ShouldBe(25248, tolerance: 1);
        scan.ActualRows.ShouldBe(25180);
    }

    [Fact]
    public void A_nested_plan_reports_every_operator_in_order()
    {
        var operators = Showplan.Operators(Plans.LoadXml(Plans.SqlServerCastToDate));

        operators.Select(op => op.PhysicalOp)
            .ShouldBe(["Nested Loops", "Constant Scan", "Compute Scalar", "Index Seek"], ignoreOrder: true);
    }

    [Fact]
    public void An_index_belongs_to_the_operator_that_touched_it()
    {
        // The Constant Scan and Compute Scalar touch no index, even though the
        // Index Seek nested beneath the Nested Loops does.
        var operators = Showplan.Operators(Plans.LoadXml(Plans.SqlServerCastToDate));

        operators.Single(op => op.PhysicalOp == "Constant Scan").Index.ShouldBeNull();
        operators.Single(op => op.PhysicalOp == "Index Seek").Index.ShouldBe("orders_placed_at_idx");
    }

    [Fact]
    public void SQL_Server_offers_an_index_when_a_predicate_has_none()
    {
        // A feature PostgreSQL has no equivalent for: the optimiser records the
        // index it wishes had existed.
        Showplan.MissingIndexColumns(Plans.LoadXml(Plans.SqlServerMissingIndex))
            .ShouldBe(["total_cents"]);
    }

    [Fact]
    public void A_plan_that_needs_nothing_suggests_nothing()
    {
        Showplan.MissingIndexColumns(Plans.LoadXml(Plans.SqlServerDirect)).ShouldBeEmpty();
    }
}
