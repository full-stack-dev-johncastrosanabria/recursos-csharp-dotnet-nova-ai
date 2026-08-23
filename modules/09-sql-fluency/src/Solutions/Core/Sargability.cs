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
/// A B-tree index stores the column's values in order, so it can only answer
/// questions about the column itself. Wrap it in anything and the index is not
/// ignored -- it is inapplicable.
///
/// The verdict is about applicability, not about what the planner will choose:
/// a sargable predicate matching most of the table is still read sequentially.
/// </summary>
public static class Sargability
{
    private static readonly string[] Operators = ["<>", "!=", ">=", "<=", " LIKE ", "=", "<", ">"];

    public static SargabilityVerdict Classify(string predicate)
    {
        var (left, op, right) = Split(predicate);

        if (left.Contains("::", StringComparison.Ordinal))
        {
            return SargabilityVerdict.CastOnColumn;
        }

        if (left.Contains('(', StringComparison.Ordinal))
        {
            return SargabilityVerdict.FunctionOnColumn;
        }

        if (left.AsSpan().IndexOfAny("+-*/%") >= 0)
        {
            return SargabilityVerdict.ArithmeticOnColumn;
        }

        if (op is "<>" or "!=")
        {
            return SargabilityVerdict.NegatedComparison;
        }

        if (op == " LIKE " && right.Trim().Trim('\'').StartsWith('%'))
        {
            return SargabilityVerdict.LeadingWildcard;
        }

        return SargabilityVerdict.Sargable;
    }

    public static string LeftHandSide(string predicate) => Split(predicate).Left;

    public static bool CanUsePlainIndex(string predicate)
        => Classify(predicate) == SargabilityVerdict.Sargable;

    private static (string Left, string Operator, string Right) Split(string predicate)
    {
        var upper = predicate.ToUpperInvariant();

        foreach (var candidate in Operators)
        {
            var index = upper.IndexOf(candidate, StringComparison.Ordinal);
            if (index >= 0)
            {
                return (
                    predicate[..index].Trim(),
                    candidate,
                    predicate[(index + candidate.Length)..].Trim());
            }
        }

        return (predicate.Trim(), string.Empty, string.Empty);
    }
}
