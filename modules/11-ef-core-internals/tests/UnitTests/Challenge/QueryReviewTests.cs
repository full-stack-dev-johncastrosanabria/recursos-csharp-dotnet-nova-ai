using Microsoft.EntityFrameworkCore;
using Shouldly;
using Training.Module11.Challenge;
using Training.Module11.Core;

namespace Training.Module11.Tests.Challenge;

public sealed class QueryReviewTests
{
    [Fact]
    public void A_clean_read_only_query_produces_no_findings()
    {
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(LoadingStrategies.Projected(db));

        QueryReview.Of(sql, tracked: false, willBeSaved: false).ShouldBeEmpty();
    }

    [Fact]
    public void Two_collection_includes_are_reported_as_a_cartesian_product()
    {
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(SplitQueries.BothCollections(db));

        QueryReview.Of(sql, tracked: true, willBeSaved: true)
            .ShouldContain(review => review.Finding == ReviewFinding.CartesianProduct);
    }

    [Fact]
    public void One_include_is_not()
    {
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(LoadingStrategies.Eager(db));

        QueryReview.Of(sql, tracked: true, willBeSaved: true)
            .ShouldNotContain(review => review.Finding == ReviewFinding.CartesianProduct);
    }

    [Fact]
    public void A_function_wrapped_predicate_is_reported()
    {
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(TranslationBoundary.ByUpperCase(db, "ORD-1"));

        QueryReview.Of(sql, tracked: false, willBeSaved: false)
            .ShouldContain(review => review.Finding == ReviewFinding.UnsargablePredicate);
    }

    [Fact]
    public void A_function_in_the_select_list_is_not_a_predicate_problem()
    {
        // count(*) in a projection is fine. Only the WHERE clause decides
        // whether an index can be used.
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(LoadingStrategies.Projected(db));

        QueryReview.Of(sql, tracked: false, willBeSaved: false)
            .ShouldNotContain(review => review.Finding == ReviewFinding.UnsargablePredicate);
    }

    [Fact]
    public void Tracking_a_query_nothing_saves_is_reported()
    {
        QueryReview.Of("SELECT a FROM b", tracked: true, willBeSaved: false)
            .ShouldContain(review => review.Finding == ReviewFinding.TrackingOnReadOnlyQuery);
    }

    [Fact]
    public void Tracking_a_query_you_will_save_is_not()
    {
        QueryReview.Of("SELECT a FROM b", tracked: true, willBeSaved: true).ShouldBeEmpty();
    }

    [Fact]
    public void Findings_arrive_in_the_order_the_enum_declares()
    {
        using var db = ShopContext.ForSqlOnly();
        var sql = GeneratedSql.For(TranslationBoundary
            .ByUpperCase(db, "X")
            .Include(order => order.Lines)
            .Include(order => order.Payments));

        var findings = QueryReview.Of(sql, tracked: true, willBeSaved: false);

        findings.Select(review => review.Finding)
            .ShouldBe([
                ReviewFinding.CartesianProduct,
                ReviewFinding.UnsargablePredicate,
                ReviewFinding.TrackingOnReadOnlyQuery]);
    }
}
