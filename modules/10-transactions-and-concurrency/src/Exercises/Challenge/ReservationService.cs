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
/// Challenge: the capstone. Put the pieces together into something you would
/// actually ship.
///
/// Exercise 4 detects a lost update and stops. Exercise 2 knows how to come
/// back. Neither is useful alone: detection without retry tells nine customers
/// out of ten that they lost a race they did not know they were in, and retry
/// without detection is just the original bug, four times.
///
/// ReserveAsync loops at most RetryPolicy.MaxAttempts times:
///
///   Reserved      -> Reserved, immediately
///   OutOfStock    -> OutOfStock, immediately. The shelf is empty; waiting will
///                    not fill it, and retrying an empty shelf is how you turn
///                    a fast "no" into a slow one.
///   NoSuchSku     -> NoSuchSku, immediately
///   LostTheRace   -> back off and try again
///
/// and returns GaveUp if it never won. Back off with RetryPolicy.DelayFor,
/// awaiting delay BETWEEN attempts and not after the last -- the same shape as
/// exercise 2, for the same reason.
/// </summary>
public static class ReservationService
{
    public static Task<ReservationOutcome> ReserveAsync(
        IStockStore store,
        string sku,
        Func<TimeSpan, Task> delay)
        => throw new NotImplementedException();
}
