namespace Training.Module04.Challenge;

public sealed record LineItem(string Sku, int Quantity, decimal UnitPrice);

/// <summary>
/// Turns invoice lines into something a human reads.
///
/// Describe matches on the shape of the collection rather than branching on
/// Count and indexing afterwards. The arms read as the cases themselves —
/// none, one, two, more — and the compiler checks that the indexes each arm
/// binds actually exist, which is the part a Count check cannot give you.
///
/// Discounted returns a copy. `with` is not a convenience here: mutating the
/// line in place would rewrite data underneath every caller still holding a
/// reference to it, and that is precisely the class of bug records exist to
/// make difficult.
/// </summary>
public static class InvoiceProjection
{
    public static string Describe(IReadOnlyList<LineItem> lines)
        => lines switch
        {
            [] => "empty",
            [var only] => $"{only.Sku} only",
            [var first, var second] => $"{first.Sku} and {second.Sku}",
            _ => $"{lines[0].Sku} and {lines.Count - 1} more",
        };

    public static decimal Total(IReadOnlyList<LineItem> lines)
        => lines.Sum(line => line.Quantity * line.UnitPrice);

    public static LineItem Discounted(LineItem line, decimal discount)
        => line with { UnitPrice = line.UnitPrice * (1 - discount) };
}
