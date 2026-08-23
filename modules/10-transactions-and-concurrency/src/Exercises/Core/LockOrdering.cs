namespace Training.Module10.Core;

/// <summary>
/// Exercise: make deadlock impossible rather than survivable.
///
/// A deadlock needs a cycle: one transaction holding A and wanting B while
/// another holds B and wants A. PostgreSQL detects that and kills one of them
/// with 40P01, which your retry loop handles -- so a deadlock is survivable.
/// It is still worth preventing, because detection takes a second of waiting by
/// default and the victim is chosen for the database's convenience, not yours.
///
/// The cycle cannot form if every transaction takes its locks in the same
/// order. That is the whole technique, and it costs nothing: sort the resources
/// before you touch them.
///
/// Order returns the resources sorted ordinally, with duplicates removed --
/// deterministic, so two processes that have never met still agree.
///
/// CouldDeadlock takes two acquisition sequences and reports whether they could
/// form a cycle: true when some pair of resources appears in BOTH sequences in
/// opposite relative order. Sequences sharing nothing, or agreeing on the order
/// of everything they share, cannot deadlock with each other.
/// </summary>
public static class LockOrdering
{
    public static IReadOnlyList<string> Order(IEnumerable<string> resources)
        => throw new NotImplementedException();

    public static bool CouldDeadlock(IReadOnlyList<string> first, IReadOnlyList<string> second)
        => throw new NotImplementedException();
}
