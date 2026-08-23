using Shouldly;
using Training.Module09.Core;

namespace Training.Module09.Tests.Core;

public sealed class ScanStrategyTests
{
    [Fact]
    public void A_function_wrapped_predicate_falls_back_to_a_sequential_scan()
    {
        var access = Only(Plans.FunctionWrapped);

        access.Relation.ShouldBe("orders");
        access.Method.ShouldBe(AccessMethod.SequentialScan);
        access.Index.ShouldBeNull();
    }

    [Fact]
    public void And_reads_the_entire_table_to_return_four_rows()
    {
        // The number this module exists to make you look at. Same answer, same
        // four rows, and 200,000 rows of work to find them.
        var access = Only(Plans.FunctionWrapped);

        access.RowsReturned.ShouldBe(4);
        access.RowsExamined.ShouldBe(200_000);
    }

    [Fact]
    public void The_sargable_form_of_the_same_lookup_reads_four()
    {
        var access = Only(Plans.Direct);

        access.Method.ShouldBe(AccessMethod.BitmapScan);
        access.Index.ShouldBe("orders_customer_email_idx");
        access.RowsExamined.ShouldBe(4);
    }

    [Fact]
    public void An_expression_index_rescues_the_original_query_unchanged()
    {
        var access = Only(Plans.ExpressionIndex);

        access.Method.ShouldBe(AccessMethod.BitmapScan);
        access.Index.ShouldBe("orders_lower_email_idx");
        access.RowsExamined.ShouldBe(4);
    }

    [Fact]
    public void A_cast_on_the_column_is_a_function_too()
    {
        var access = Only(Plans.CastToDate);

        access.Method.ShouldBe(AccessMethod.SequentialScan);
        access.RowsExamined.ShouldBe(200_000);
    }

    [Fact]
    public void The_range_rewrite_of_that_cast_uses_the_index()
    {
        var access = Only(Plans.Range);

        access.Method.ShouldBe(AccessMethod.BitmapScan);
        access.Index.ShouldBe("orders_placed_at_idx");
    }

    [Fact]
    public void An_index_only_scan_is_reported_as_its_own_method()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.PerRowSubquery));

        ScanStrategy.Describe(root)
            .ShouldContain(access => access.Method == AccessMethod.IndexOnlyScan);
    }

    [Fact]
    public void A_join_describes_every_relation_it_touched()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.HashJoin));

        ScanStrategy.Describe(root)
            .Select(access => access.Relation)
            .ShouldBe(["customers", "orders"], ignoreOrder: true);
    }

    [Fact]
    public void Sequential_relations_are_listed_on_their_own()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.FunctionWrapped));

        ScanStrategy.RelationsReadSequentially(root).ShouldBe(["orders"]);
    }

    [Fact]
    public void A_fully_indexed_plan_has_none()
    {
        var root = PlanTree.Parse(Plans.Load(Plans.Direct));

        ScanStrategy.RelationsReadSequentially(root).ShouldBeEmpty();
    }

    private static RelationAccess Only(string fixture)
        => ScanStrategy.Describe(PlanTree.Parse(Plans.Load(fixture))).Single();
}
