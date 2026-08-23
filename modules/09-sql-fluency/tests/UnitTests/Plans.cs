namespace Training.Module09.Tests;

/// <summary>
/// The captured plans every test in this module reads.
///
/// These are not written by hand. Each one is the verbatim output of
/// EXPLAIN (ANALYZE, COSTS, TIMING, FORMAT JSON) against PostgreSQL 18 with a
/// 200,000-row orders table, captured once and committed. That matters: a plan
/// invented to make an exercise work would teach a shape the planner never
/// actually produces, and the numbers in it -- the estimates, the loop counts,
/// the timings -- are exactly the numbers the exercises are about.
/// </summary>
public static class Plans
{
    /// <summary>lower(customer_email) = '...' -- the index cannot be used.</summary>
    public const string FunctionWrapped = "seq-scan-function-wrapped";

    /// <summary>customer_email = '...' -- the same lookup, sargable.</summary>
    public const string Direct = "index-scan-direct";

    /// <summary>The first query again, after CREATE INDEX ON orders (lower(customer_email)).</summary>
    public const string ExpressionIndex = "index-scan-expression-index";

    /// <summary>placed_at::date = '...' -- a cast is a function too.</summary>
    public const string CastToDate = "seq-scan-cast-to-date";

    /// <summary>placed_at &gt;= x AND placed_at &lt; y -- the sargable rewrite.</summary>
    public const string Range = "index-scan-range";

    /// <summary>A correlated subquery: one index lookup per outer row, 200 times.</summary>
    public const string PerRowSubquery = "nested-loop-per-row-subquery";

    /// <summary>total_cents % 4 = 1 -- an expression the planner has no statistics for.</summary>
    public const string BadEstimate = "estimate-far-below-actual";

    /// <summary>A join, for walking a plan with more than one branch.</summary>
    public const string HashJoin = "hash-join-with-filter";

    public static string Load(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "plans", name + ".json"));
}
