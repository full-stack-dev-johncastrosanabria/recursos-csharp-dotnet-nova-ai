using Training.Module09.Core;

namespace Training.Module09.Challenge;

/// <summary>The two engines this module has real plans from.</summary>
public enum SqlEngine
{
    PostgreSql,
    SqlServer,
}

/// <summary>
/// The rule is universal; its exceptions are not. An index cannot answer
/// questions about a computed value -- but which computations an optimiser
/// will rewrite before giving up is a fact about that optimiser.
/// </summary>
public static class EngineSargability
{
    public static bool CanSeek(SqlEngine engine, string predicate)
    {
        // SQL Server rewrites a date cast into a range over the raw column,
        // because the cast preserves order. PostgreSQL does not.
        if (engine == SqlEngine.SqlServer && IsDateCast(Sargability.LeftHandSide(predicate)))
        {
            return true;
        }

        return Sargability.CanUsePlainIndex(predicate);
    }

    public static bool EnginesAgree(string predicate)
        => CanSeek(SqlEngine.PostgreSql, predicate) == CanSeek(SqlEngine.SqlServer, predicate);

    private static bool IsDateCast(string left)
        => left.Contains("AS date", StringComparison.OrdinalIgnoreCase)
            || left.EndsWith("::date", StringComparison.OrdinalIgnoreCase);
}
