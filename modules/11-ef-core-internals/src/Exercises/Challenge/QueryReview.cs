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

/// <summary>
/// Challenge: the capstone. Review a query the way you would in a pull request,
/// using only what the previous exercises established.
///
/// Report, in the order the enum declares:
///
///   CartesianProduct when the statement contains JoinsThatMultiply or more
///   LEFT JOINs -- two collection includes in one query multiply rather than
///   add.
///
///   UnsargablePredicate when, anywhere after the first WHERE, a quoted
///   identifier appears as the argument of a function call: letters or
///   underscores, an opening parenthesis, an optional alias and dot, a quoted
///   name, a closing parenthesis.
///
///   TrackingOnReadOnlyQuery when the query tracks its results and nothing will
///   be saved from them.
///
/// A clean query returns nothing. That is the finding that matters most: an
/// analyser which always has an opinion is one nobody reads.
/// </summary>
public static class QueryReview
{
    public const int JoinsThatMultiply = 2;

    public static IReadOnlyList<Review> Of(string sql, bool tracked, bool willBeSaved)
        => throw new NotImplementedException();
}
