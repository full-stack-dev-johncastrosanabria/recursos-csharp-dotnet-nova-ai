namespace Training.Module04.Challenge;

public sealed record LineItem(string Sku, int Quantity, decimal UnitPrice);

/// <summary>
/// Turns invoice lines into something a human reads.
///
/// Challenge: Describe is a list-pattern exercise. Match on the shape of the
/// collection itself — empty, one, two, or more — rather than branching on
/// Count and indexing afterwards.
///
/// Discounted must not modify the line it is given. `with` produces a copy;
/// mutating the original rewrites data underneath every caller holding a
/// reference to it, which is the bug records exist to make hard.
/// </summary>
public static class InvoiceProjection
{
    public static string Describe(IReadOnlyList<LineItem> lines)
        => throw new NotImplementedException();

    public static decimal Total(IReadOnlyList<LineItem> lines)
        => throw new NotImplementedException();

    public static LineItem Discounted(LineItem line, decimal discount)
        => throw new NotImplementedException();
}
