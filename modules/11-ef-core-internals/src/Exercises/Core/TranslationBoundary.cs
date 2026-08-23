namespace Training.Module11.Core;

/// <summary>
/// Exercise: LINQ is not SQL, and the translation has opinions.
///
/// The half people know: some expressions cannot be translated at all, and EF
/// Core refuses them rather than silently fetching the table and filtering in
/// memory. That refusal is a feature -- it is module 04's client-side filter
/// bug, caught at the boundary.
///
/// The half people miss, and the reason this exercise exists: an expression can
/// translate PERFECTLY and still be a performance bug.
/// Where(o =&gt; o.Reference.ToUpper() == x) becomes upper("Reference") = x,
/// which is exactly the unsargable predicate module 09 is about. Nothing warns
/// you. The LINQ is idiomatic, the SQL is correct, the index on Reference
/// cannot be used, and the query degrades as the table grows.
///
/// ByUpperCase compares o.Reference.ToUpper() to the argument. ByExactMatch
/// compares o.Reference directly. ByOrdinalIgnoreCase uses
/// string.Equals(a, b, StringComparison.OrdinalIgnoreCase) -- which is what the
/// CA1862 analyser will tell you to write, and which has no SQL translation at
/// all. Good advice for in-memory code; a runtime failure inside a query.
///
/// WrapsColumnInFunction reports whether the SQL uses the named column as the
/// argument of a function call: a run of letters or underscores, an opening
/// parenthesis, an optional table alias and dot, the column in double quotes,
/// and a closing parenthesis.
/// </summary>
public static class TranslationBoundary
{
    public static IQueryable<Order> ByUpperCase(ShopContext db, string reference)
        => throw new NotImplementedException();

    public static IQueryable<Order> ByExactMatch(ShopContext db, string reference)
        => throw new NotImplementedException();

    public static IQueryable<Order> ByOrdinalIgnoreCase(ShopContext db, string reference)
        => throw new NotImplementedException();

    public static bool WrapsColumnInFunction(string sql, string column)
        => throw new NotImplementedException();
}
