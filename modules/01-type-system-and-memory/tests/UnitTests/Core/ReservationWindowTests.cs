using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class ReservationWindowTests
{
    private static readonly DateTimeOffset Noon = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void End_is_start_plus_duration()
    {
        new ReservationWindow(Noon, TimeSpan.FromHours(2)).End.ShouldBe(Noon.AddHours(2));
    }

    [Fact]
    public void Extending_returns_a_new_window_and_leaves_the_original_alone()
    {
        var original = new ReservationWindow(Noon, TimeSpan.FromHours(1));

        var extended = original.ExtendBy(TimeSpan.FromHours(1));

        extended.Duration.ShouldBe(TimeSpan.FromHours(2));
        original.Duration.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Overlapping_windows_are_detected()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(2));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(2));

        first.Overlaps(second).ShouldBeTrue();
    }

    [Fact]
    public void Touching_windows_do_not_overlap()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(1));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(1));

        first.Overlaps(second).ShouldBeFalse();
    }

    [Fact]
    public void Overlap_is_symmetric()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(2));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(2));

        first.Overlaps(second).ShouldBe(second.Overlaps(first));
    }
}
