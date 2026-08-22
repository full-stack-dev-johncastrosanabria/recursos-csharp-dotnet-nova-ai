using Shouldly;
using Training.Module02.Core;

namespace Training.Module02.Tests.Core;

public sealed class ShipmentBatchTests
{
    private sealed class Recorder(string name, List<string> log, bool throws = false) : IDisposable
    {
        public void Dispose()
        {
            log.Add(name);
            if (throws)
            {
                throw new InvalidOperationException($"{name} failed to close");
            }
        }
    }

    [Fact]
    public void Disposes_everything_it_owns()
    {
        var log = new List<string>();
        var batch = new ShipmentBatch();
        batch.Add(new Recorder("a", log));
        batch.Add(new Recorder("b", log));

        batch.Dispose();

        log.Count.ShouldBe(2);
    }

    [Fact]
    public void Disposes_in_reverse_order_of_acquisition()
    {
        // Resources are released in the opposite order they were taken, because
        // a later one may depend on an earlier one still being open.
        var log = new List<string>();
        var batch = new ShipmentBatch();
        batch.Add(new Recorder("first", log));
        batch.Add(new Recorder("second", log));
        batch.Add(new Recorder("third", log));

        batch.Dispose();

        log.ShouldBe(["third", "second", "first"]);
    }

    [Fact]
    public void One_failing_resource_does_not_strand_the_others()
    {
        var log = new List<string>();
        var batch = new ShipmentBatch();
        batch.Add(new Recorder("a", log));
        batch.Add(new Recorder("b", log, throws: true));
        batch.Add(new Recorder("c", log));

        Should.Throw<AggregateException>(batch.Dispose);

        log.ShouldBe(["c", "b", "a"]);
    }

    [Fact]
    public void The_failures_are_reported_rather_than_swallowed()
    {
        var log = new List<string>();
        var batch = new ShipmentBatch();
        batch.Add(new Recorder("a", log, throws: true));
        batch.Add(new Recorder("b", log, throws: true));

        var error = Should.Throw<AggregateException>(batch.Dispose);

        error.InnerExceptions.Count.ShouldBe(2);
    }

    [Fact]
    public void Disposing_twice_does_not_dispose_its_children_twice()
    {
        var log = new List<string>();
        var batch = new ShipmentBatch();
        batch.Add(new Recorder("a", log));

        batch.Dispose();
        batch.Dispose();

        log.Count.ShouldBe(1);
    }

    [Fact]
    public void Adding_after_disposal_throws()
    {
        var batch = new ShipmentBatch();
        batch.Dispose();

        Should.Throw<ObjectDisposedException>(() => batch.Add(new Recorder("late", [])));
    }
}
