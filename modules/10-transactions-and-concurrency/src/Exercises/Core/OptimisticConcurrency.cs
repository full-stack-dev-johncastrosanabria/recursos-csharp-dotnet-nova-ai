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
/// Exercise: detect the lost update instead of preventing it.
///
/// The pessimistic answer is to lock the row while you think. The optimistic
/// answer is to not lock anything, and instead make the write refuse to land if
/// the row moved underneath you: carry a version, and write only if it is still
/// the version you read. Nothing blocks, and a collision is detected rather
/// than prevented.
///
/// The detection is the row count. An UPDATE whose WHERE clause no longer
/// matches affects zero rows and raises nothing at all -- so code that ignores
/// the row count has an optimistic concurrency scheme that detects nothing.
/// That is the single most common way this is got wrong.
///
/// TryReserveAsync reads the row, and:
///
///   no row            -> NoSuchSku
///   quantity zero     -> OutOfStock
///   otherwise         -> write quantity - 1 against the version it read;
///                        1 row affected is Reserved, 0 is LostTheRace.
///
/// Note what this does NOT do: retry. One attempt against ten buyers reserves
/// one unit and tells nine of them they lost, which is correct and useless.
/// Exercise 10 puts the retry loop around it.
/// </summary>
public static class OptimisticConcurrency
{
    public static Task<ReserveResult> TryReserveAsync(IStockStore store, string sku)
        => throw new NotImplementedException();
}
