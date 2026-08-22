namespace Training.Module01.Core;

/// <summary>
/// A window during which stock is held for an order.
///
/// `readonly` on the struct tells the compiler no member mutates state, so
/// passing it by `in` needs no defensive copy. Drop the `readonly` and every
/// `in` parameter silently becomes a copy per call.
/// </summary>
public readonly struct ReservationWindow
{
    public ReservationWindow(DateTimeOffset start, TimeSpan duration)
    {
        Start = start;
        Duration = duration;
    }

    public DateTimeOffset Start { get; }

    public TimeSpan Duration { get; }

    public DateTimeOffset End => Start + Duration;

    public ReservationWindow ExtendBy(TimeSpan extra) => new(Start, Duration + extra);

    public bool Overlaps(in ReservationWindow other) => Start < other.End && other.Start < End;
}
