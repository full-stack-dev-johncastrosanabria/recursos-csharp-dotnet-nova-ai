using Training.Module10.Core;

namespace Training.Module10.Challenge;

/// <summary>How a reservation ended, after however many attempts it took.</summary>
public enum ReservationOutcome
{
    Reserved,
    OutOfStock,
    NoSuchSku,
    GaveUp,
}

/// <summary>
/// Detection plus retry. Either alone is useless: detection without retry tells
/// nine customers in ten that they lost, and retry without detection is the
/// original bug repeated.
/// </summary>
public static class ReservationService
{
    public static async Task<ReservationOutcome> ReserveAsync(
        IStockStore store,
        string sku,
        Func<TimeSpan, Task> delay)
    {
        for (var attempt = 1; attempt <= RetryPolicy.MaxAttempts; attempt++)
        {
            var result = await OptimisticConcurrency.TryReserveAsync(store, sku);

            switch (result)
            {
                case ReserveResult.Reserved:
                    return ReservationOutcome.Reserved;

                // Waiting will not refill the shelf.
                case ReserveResult.OutOfStock:
                    return ReservationOutcome.OutOfStock;

                case ReserveResult.NoSuchSku:
                    return ReservationOutcome.NoSuchSku;

                default:
                    if (attempt < RetryPolicy.MaxAttempts)
                    {
                        await delay(RetryPolicy.DelayFor(attempt, Random.Shared.NextDouble()));
                    }

                    break;
            }
        }

        return ReservationOutcome.GaveUp;
    }
}
