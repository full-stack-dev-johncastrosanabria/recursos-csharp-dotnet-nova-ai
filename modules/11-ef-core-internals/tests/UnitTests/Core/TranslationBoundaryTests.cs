using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.Tests.Core;

public sealed class TranslationBoundaryTests
{
    [Fact]
    public void ToUpper_translates_perfectly_into_an_unsargable_predicate()
    {
        // Module 09's real-world case, generated for you by idiomatic LINQ.
        // Nothing here is an error. The index on Reference simply cannot help.
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(TranslationBoundary.ByUpperCase(db, "ORD-1"));

        GeneratedSql.Mentions(sql, "upper(").ShouldBeTrue();
        TranslationBoundary.WrapsColumnInFunction(sql, "Reference").ShouldBeTrue();
    }

    [Fact]
    public void A_direct_comparison_leaves_the_column_alone()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(TranslationBoundary.ByExactMatch(db, "ORD-1"));

        TranslationBoundary.WrapsColumnInFunction(sql, "Reference").ShouldBeFalse();
        GeneratedSql.Mentions(sql, "\"Reference\" =").ShouldBeTrue();
    }

    [Fact]
    public void Both_queries_are_correct_and_only_one_can_use_an_index()
    {
        // The detection gap in one assertion: same rows, same shape, no error.
        using var db = ShopContext.ForSqlOnly();

        var upper = GeneratedSql.For(TranslationBoundary.ByUpperCase(db, "ORD-1"));
        var exact = GeneratedSql.For(TranslationBoundary.ByExactMatch(db, "ORD-1"));

        GeneratedSql.Mentions(upper, "WHERE").ShouldBeTrue();
        GeneratedSql.Mentions(exact, "WHERE").ShouldBeTrue();
        TranslationBoundary.WrapsColumnInFunction(upper, "Reference")
            .ShouldNotBe(TranslationBoundary.WrapsColumnInFunction(exact, "Reference"));
    }

    [Fact]
    public void The_check_only_matches_the_column_it_was_asked_about()
    {
        using var db = ShopContext.ForSqlOnly();

        var sql = GeneratedSql.For(TranslationBoundary.ByUpperCase(db, "ORD-1"));

        TranslationBoundary.WrapsColumnInFunction(sql, "Total").ShouldBeFalse();
    }

    [Fact]
    public void The_comparison_the_analyser_recommends_cannot_be_translated_at_all()
    {
        // CA1862 tells you to write this, and for in-memory code it is right.
        // In an expression tree it has no SQL equivalent, so EF refuses --
        // which is the good outcome. The alternative would be fetching the
        // table and filtering in memory, which is module 04's bug.
        using var db = ShopContext.ForSqlOnly();

        Should.Throw<InvalidOperationException>(
            () => GeneratedSql.For(TranslationBoundary.ByOrdinalIgnoreCase(db, "ORD-1")));
    }

    [Fact]
    public void A_bare_column_in_a_select_list_is_not_a_function_call()
    {
        TranslationBoundary.WrapsColumnInFunction("""SELECT o."Reference" FROM "Orders" AS o""", "Reference")
            .ShouldBeFalse();
        TranslationBoundary.WrapsColumnInFunction("""SELECT upper(o."Reference") FROM "Orders" AS o""", "Reference")
            .ShouldBeTrue();
    }
}
