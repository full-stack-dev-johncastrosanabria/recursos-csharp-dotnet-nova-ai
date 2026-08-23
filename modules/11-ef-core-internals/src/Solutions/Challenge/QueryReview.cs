using System.Text.RegularExpressions;
using Training.Module11.Core;

namespace Training.Module11.Challenge;

/// <summary>Something worth saying about a query before it ships.</summary>
public enum ReviewFinding
{
    /// <summary>Two or more collection joins in one statement multiply their rows.</summary>
    CartesianProduct,

    /// <summary>The WHERE clause wraps a column in a function, so no index on it can be used.</summary>
    UnsargablePredicate,

    /// <summary>Entities are being tracked by a query whose results are never saved.</summary>
    TrackingOnReadOnlyQuery,
}

/// <summary>One finding, with enough detail to act on.</summary>
public sealed record Review(ReviewFinding Finding, string Detail);

/// <summary>Reviewing a query the way you would in a pull request.</summary>
public static partial class QueryReview
{
    public const int JoinsThatMultiply = 2;

    public static IReadOnlyList<Review> Of(string sql, bool tracked, bool willBeSaved)
    {
        var findings = new List<Review>();

        var joins = GeneratedSql.Occurrences(sql, "LEFT JOIN");
        if (joins >= JoinsThatMultiply)
        {
            findings.Add(new Review(
                ReviewFinding.CartesianProduct,
                $"{joins} collection joins in one statement; consider AsSplitQuery or a projection."));
        }

        var where = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        if (where >= 0 && FunctionOnColumn().IsMatch(sql[where..]))
        {
            findings.Add(new Review(
                ReviewFinding.UnsargablePredicate,
                "A column is wrapped in a function in the WHERE clause; no index on it can be used."));
        }

        if (tracked && !willBeSaved)
        {
            findings.Add(new Review(
                ReviewFinding.TrackingOnReadOnlyQuery,
                "Nothing is saved from this query; AsNoTracking avoids the bookkeeping."));
        }

        return findings;
    }

    [GeneratedRegex("[A-Za-z_]+\\(\\s*(?:[A-Za-z0-9_]+\\.)?\"[^\"]+\"\\s*\\)")]
    private static partial Regex FunctionOnColumn();
}
