
namespace Training.Module09.Challenge;

/// <summary>What to do about a predicate that cannot use a plain index.</summary>
public enum Remedy
{
    CreatePlainIndex,
    CreateExpressionIndex,
    RewriteAsRange,
    NoBTreeIndexHelps,
}

/// <summary>A remedy and the statement or instruction that carries it out.</summary>
public sealed record Advice(Remedy Remedy, string Statement);

/// <summary>
/// Challenge: turn a verdict into a decision.
///
/// Knowing a predicate is not sargable is half the job. The other half is that
/// the answer is not always "add an index" -- an index has a write cost and a
/// storage cost on every insert forever, and some of these are better fixed in
/// the query.
///
/// From the Sargability verdict:
///
///   Sargable            -> CreatePlainIndex, "CREATE INDEX ON {table} ({lhs});"
///   FunctionOnColumn    -> CreateExpressionIndex, the same statement with the
///                          whole expression as the indexed key.
///   ArithmeticOnColumn  -> CreateExpressionIndex, likewise.
///   CastOnColumn        -> RewriteAsRange. A date cast is the one case where
///                          the query is simply wrong: a half-open range over
///                          the raw column uses the index you already have, and
///                          costs nothing to maintain.
///   NegatedComparison   -> NoBTreeIndexHelps
///   LeadingWildcard     -> NoBTreeIndexHelps
///
/// For the two that are not statements, Statement carries a one-line
/// instruction; the tests check only that it is not empty.
/// </summary>
public static class IndexAdvice
{
    public static Advice For(string table, string predicate) => throw new NotImplementedException();
}
