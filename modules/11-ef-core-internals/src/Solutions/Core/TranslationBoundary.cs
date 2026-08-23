using System.Text.RegularExpressions;

namespace Training.Module11.Core;

/// <summary>
/// LINQ is not SQL, and an expression can translate perfectly and still be a
/// performance bug.
/// </summary>
public static class TranslationBoundary
{
    // CA1862 suggests string.Equals(..., StringComparison.OrdinalIgnoreCase)
    // here, and it is right about ordinary in-memory code. Inside an expression
    // tree it is exactly wrong: that overload has no SQL translation, so taking
    // the analyser's advice turns a working query into a runtime failure. See
    // ByOrdinalIgnoreCase below, and the test that asserts it.
#pragma warning disable CA1862
    public static IQueryable<Order> ByUpperCase(ShopContext db, string reference)
        => db.Orders.Where(order => order.Reference.ToUpper() == reference);
#pragma warning restore CA1862

    public static IQueryable<Order> ByExactMatch(ShopContext db, string reference)
        => db.Orders.Where(order => order.Reference == reference);

    public static IQueryable<Order> ByOrdinalIgnoreCase(ShopContext db, string reference)
        => db.Orders.Where(order =>
            string.Equals(order.Reference, reference, StringComparison.OrdinalIgnoreCase));

    public static bool WrapsColumnInFunction(string sql, string column)
        => Regex.IsMatch(
            sql,
            $"""[A-Za-z_]+\(\s*(?:[A-Za-z0-9_]+\.)?"{Regex.Escape(column)}"\s*\)""",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));
}
