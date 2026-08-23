using Training.Module11.Core;

namespace Training.Module11.Challenge;

/// <summary>Everything SaveChanges is about to do, before it does it.</summary>
public sealed record PendingWork(int Inserts, int Updates, int Deletes, IReadOnlyList<string> ModifiedProperties);

/// <summary>
/// Challenge: ask the context what it is about to write.
///
/// SaveChanges is not magic and it is not opaque. Everything it will do is
/// already sitting in ChangeTracker.Entries(), and you can read it -- which is
/// worth doing in exactly the situations where people instead guess: a save
/// that updates more rows than expected, a save that updates none, an audit
/// log, or a test that wants to assert intent rather than outcome.
///
/// Summarise counts entries by state and lists the names of every property
/// marked modified, across all entities, sorted and without duplicates. Only
/// properties on Modified entities count -- an Added entity is going to have
/// all of its columns written anyway, so calling them "modified" would be
/// noise.
/// </summary>
public static class ChangeTrackerAudit
{
    public static PendingWork Summarise(ShopContext db) => throw new NotImplementedException();
}
