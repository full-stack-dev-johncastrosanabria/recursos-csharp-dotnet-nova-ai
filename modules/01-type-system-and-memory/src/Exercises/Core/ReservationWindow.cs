namespace Training.Module01.Core;

/// <summary>
/// A window during which stock is held for an order.
///
/// Exercise: a readonly struct that cannot be mutated in place. ExtendBy
/// returns a new window rather than changing this one, and Overlaps takes its
/// argument by `in` so no defensive copy is made. The examples folder shows
/// what happens when a struct is mutable and you forget.
/// </summary>
public readonly struct ReservationWindow
{
    public ReservationWindow(DateTimeOffset start, TimeSpan duration) => throw new NotImplementedException();

    public DateTimeOffset Start => throw new NotImplementedException();

    public TimeSpan Duration => throw new NotImplementedException();

    public DateTimeOffset End => throw new NotImplementedException();

    public ReservationWindow ExtendBy(TimeSpan extra) => throw new NotImplementedException();

    public bool Overlaps(in ReservationWindow other) => throw new NotImplementedException();
}
