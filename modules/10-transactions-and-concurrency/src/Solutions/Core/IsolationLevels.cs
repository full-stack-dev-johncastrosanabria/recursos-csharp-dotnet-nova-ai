using System.Data;

namespace Training.Module10.Core;

/// <summary>The three anomalies the SQL standard defines its levels in terms of.</summary>
public enum Anomaly
{
    /// <summary>Reading a row another transaction wrote and has not committed.</summary>
    DirtyRead,

    /// <summary>Reading the same row twice in one transaction and getting different values.</summary>
    NonRepeatableRead,

    /// <summary>Running the same query twice and finding rows that were not there before.</summary>
    PhantomRead,
}

/// <summary>
/// What each isolation level promises, and what PostgreSQL actually gives you
/// when you ask for it.
/// </summary>
public static class IsolationLevels
{
    public static IReadOnlySet<Anomaly> PermittedByStandard(IsolationLevel level) => level switch
    {
        IsolationLevel.ReadUncommitted =>
            new HashSet<Anomaly> { Anomaly.DirtyRead, Anomaly.NonRepeatableRead, Anomaly.PhantomRead },
        IsolationLevel.ReadCommitted =>
            new HashSet<Anomaly> { Anomaly.NonRepeatableRead, Anomaly.PhantomRead },
        IsolationLevel.RepeatableRead =>
            new HashSet<Anomaly> { Anomaly.PhantomRead },
        IsolationLevel.Serializable =>
            new HashSet<Anomaly>(),
        _ => throw new ArgumentOutOfRangeException(
            nameof(level), level, "The standard defines four levels."),
    };

    public static IsolationLevel EffectiveInPostgres(IsolationLevel requested)
        // Snapshots cannot contain uncommitted data, so a dirty read is not
        // something PostgreSQL can do even when asked. It does not complain,
        // and it does not correct the label either -- only behaviour differs.
        => requested == IsolationLevel.ReadUncommitted
            ? IsolationLevel.ReadCommitted
            : requested;
}
