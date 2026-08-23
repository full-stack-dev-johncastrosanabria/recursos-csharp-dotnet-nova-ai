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
/// Exercise: what each isolation level actually promises.
///
/// The standard defines its four levels by what they PERMIT, not by how they
/// are implemented -- which is why two databases can both be "READ COMMITTED"
/// and behave differently, and why the level you ask for is not always the
/// level you get.
///
/// The table, from the standard:
///
///   READ UNCOMMITTED   dirty reads, non-repeatable reads, phantoms
///   READ COMMITTED     non-repeatable reads, phantoms
///   REPEATABLE READ    phantoms
///   SERIALIZABLE       nothing
///
/// PermittedByStandard returns that set. Any level outside those four throws
/// ArgumentOutOfRangeException.
///
/// EffectiveInPostgres is the second half, and it is the practical one.
/// PostgreSQL implements isolation with snapshots rather than read locks, and
/// a snapshot never contains uncommitted data -- so a dirty read is not
/// something it CAN do. Ask for READ UNCOMMITTED and you get READ COMMITTED
/// behaviour.
///
/// Note carefully what that does NOT mean. The request is not rejected, and it
/// is not corrected: SHOW transaction_isolation cheerfully reports "read
/// uncommitted" back to you. Only behaviour gives it away, which is why this
/// method is about what the level DOES rather than what the server calls it.
/// Every other level is honoured as asked. The integration tier asserts both
/// halves against a live server.
/// </summary>
public static class IsolationLevels
{
    public static IReadOnlySet<Anomaly> PermittedByStandard(IsolationLevel level)
        => throw new NotImplementedException();

    public static IsolationLevel EffectiveInPostgres(IsolationLevel requested)
        => throw new NotImplementedException();
}
