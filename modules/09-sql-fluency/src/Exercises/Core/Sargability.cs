namespace Training.Module09.Core;

/// <summary>Why a predicate can or cannot be served by a plain B-tree index.</summary>
public enum SargabilityVerdict
{
    Sargable,
    CastOnColumn,
    FunctionOnColumn,
    ArithmeticOnColumn,
    NegatedComparison,
    LeadingWildcard,
}

/// <summary>
/// Exercise: the module's real-world case, as a rule you can apply by eye.
///
/// A B-tree index stores the COLUMN'S values, in order. An index lookup is a
/// search through those values, so it only works when the query asks about the
/// column itself. Wrap the column in anything -- a function, a cast, a piece of
/// arithmetic -- and you are asking about a value the index does not contain,
/// so PostgreSQL has no choice but to compute it for every row. The index is
/// not "ignored"; it is inapplicable.
///
/// Read the verdict as "could this index apply", not "will it be used".
/// Sargability is necessary and not sufficient: a sargable predicate matching
/// most of the table is still read sequentially, because thousands of random
/// heap fetches cost more than reading the table in order. The integration
/// tier asserts both halves against a live server.
///
/// The rules, applied in this order:
///
///   Split the predicate at the first comparison operator, checking the
///   two-character forms first: "&lt;&gt;", "!=", "&gt;=", "&lt;=", then LIKE
///   (any case, surrounded by spaces), then "=", "&lt;", "&gt;".
///   Left-hand side contains "::"          -> CastOnColumn
///   Left-hand side contains "("           -> FunctionOnColumn
///   Left-hand side contains + - * / or %  -> ArithmeticOnColumn
///   Operator is "&lt;&gt;" or "!="        -> NegatedComparison
///   LIKE whose literal begins with %      -> LeadingWildcard
///   Otherwise                             -> Sargable
///
/// LeftHandSide returns that left part, trimmed -- which is what you would put
/// in an index if you decided to build one for it.
/// </summary>
public static class Sargability
{
    public static SargabilityVerdict Classify(string predicate) => throw new NotImplementedException();

    public static string LeftHandSide(string predicate) => throw new NotImplementedException();

    public static bool CanUsePlainIndex(string predicate) => throw new NotImplementedException();
}
