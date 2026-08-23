namespace Training.Module09.Challenge;

/// <summary>The two engines this module has real plans from.</summary>
public enum SqlEngine
{
    PostgreSql,
    SqlServer,
}

/// <summary>
/// Challenge: the rule is universal, its exceptions are not.
///
/// "Never wrap a column in a function" is the right instinct and it is stated
/// too absolutely almost everywhere, including in section 2 of this guide. The
/// principle really is universal -- an index stores the column's values, so it
/// cannot answer questions about something computed from them. What is NOT
/// universal is which computations an optimiser is prepared to rewrite for you
/// before it gives up.
///
/// The captured plans show one such difference, and it reverses the answer:
///
///   CAST(placed_at AS date) = @d  -- SQL Server SEEKS. It knows that casting
///   a datetime2 to date preserves order, so it rewrites the predicate into a
///   range over the raw column and seeks that. The plan gives it away: a
///   Constant Scan feeding a Nested Loops feeding an Index Seek.
///
///   placed_at::date = @d          -- PostgreSQL SCANS. It has no such rule,
///   and reads the whole table.
///
/// Everything else in section 2's table behaves the same on both. YEAR(col) is
/// a scan on SQL Server too, because a year does not preserve enough order to
/// rewrite -- many datetimes map to one year, and the optimiser would have to
/// invent the range itself.
///
/// CanSeek answers for one engine. EnginesAgree is true when both give the same
/// answer, which is the question worth asking before you carry a rule of thumb
/// from one job to the next.
///
/// Recognise a date cast in either dialect: CAST(x AS date) or x::date. Every
/// other predicate falls through to the engine-independent rule in
/// Sargability.CanUsePlainIndex.
/// </summary>
public static class EngineSargability
{
    public static bool CanSeek(SqlEngine engine, string predicate) => throw new NotImplementedException();

    public static bool EnginesAgree(string predicate) => throw new NotImplementedException();
}
