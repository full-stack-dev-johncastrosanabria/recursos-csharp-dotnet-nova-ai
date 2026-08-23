namespace Training.Module10.Core;

/// <summary>A stock row, with the version that guards it.</summary>
public sealed record StockRow(string Sku, int Quantity, int Version);

/// <summary>The narrow slice of storage this exercise needs.</summary>
public interface IStockStore
{
    Task<StockRow?> ReadAsync(string sku);

    /// <summary>
    /// The conditional write. Returns the number of rows affected: 1 when the
    /// version still matched, 0 when somebody else got there first. In SQL this
    /// is UPDATE ... WHERE sku = @sku AND version = @expected.
    /// </summary>
    Task<int> UpdateIfVersionMatchesAsync(string sku, int quantity, int expectedVersion);
}

/// <summary>How one attempt to reserve a unit ended.</summary>
public enum ReserveResult
{
    Reserved,
    OutOfStock,
    LostTheRace,
    NoSuchSku,
}

/// <summary>
/// Detecting the lost update instead of preventing it: carry a version, and
/// write only if the row still has the version you read.
/// </summary>
public static class OptimisticConcurrency
{
    public static async Task<ReserveResult> TryReserveAsync(IStockStore store, string sku)
    {
        var row = await store.ReadAsync(sku);

        if (row is null)
        {
            return ReserveResult.NoSuchSku;
        }

        if (row.Quantity <= 0)
        {
            return ReserveResult.OutOfStock;
        }

        var affected = await store.UpdateIfVersionMatchesAsync(sku, row.Quantity - 1, row.Version);

        // Zero rows is the whole mechanism. It is not an error, and nothing
        // throws -- which is why ignoring the row count detects nothing.
        return affected == 1 ? ReserveResult.Reserved : ReserveResult.LostTheRace;
    }
}
