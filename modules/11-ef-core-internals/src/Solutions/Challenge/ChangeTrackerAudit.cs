using Microsoft.EntityFrameworkCore;
using Training.Module11.Core;

namespace Training.Module11.Challenge;

/// <summary>Everything SaveChanges is about to do, before it does it.</summary>
public sealed record PendingWork(int Inserts, int Updates, int Deletes, IReadOnlyList<string> ModifiedProperties);

/// <summary>
/// Asking the context what it is about to write. It is all in
/// ChangeTracker.Entries(); nothing here needs a database.
/// </summary>
public static class ChangeTrackerAudit
{
    public static PendingWork Summarise(ShopContext db)
    {
        var entries = db.ChangeTracker.Entries().ToArray();

        var modified = entries
            .Where(entry => entry.State == EntityState.Modified)
            .SelectMany(entry => entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return new PendingWork(
            entries.Count(entry => entry.State == EntityState.Added),
            entries.Count(entry => entry.State == EntityState.Modified),
            entries.Count(entry => entry.State == EntityState.Deleted),
            modified);
    }
}
