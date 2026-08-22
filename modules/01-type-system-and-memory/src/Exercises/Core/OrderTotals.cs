namespace Training.Module01.Core;

/// <summary>
/// Exercise: SumViaInterface below is given to you and it allocates. Write
/// SumWithoutAllocating so it produces the same total while allocating nothing.
/// The test measures it — you cannot talk your way past this one.
/// </summary>
public static class OrderTotals
{
    public static decimal SumViaInterface(IReadOnlyList<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }

    public static decimal SumWithoutAllocating(List<Money> lines) => throw new NotImplementedException();
}
