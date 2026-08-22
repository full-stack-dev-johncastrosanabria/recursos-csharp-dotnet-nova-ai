using System.Text;
using Shouldly;
using Training.Module02.Challenge;

namespace Training.Module02.Tests.Challenge;

public sealed class ShipmentLabelWriterTests
{
    [Fact]
    public void Labels_reach_the_stream()
    {
        using var output = new MemoryStream();

        using (var writer = new ShipmentLabelWriter(output, leaveOpen: true))
        {
            writer.Write("SHIP-1");
        }

        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("SHIP-1\n");
    }

    [Fact]
    public void An_owned_stream_is_closed_with_the_writer()
    {
        var output = new MemoryStream();
        var writer = new ShipmentLabelWriter(output, leaveOpen: false);

        writer.Write("SHIP-2");
        writer.Dispose();

        output.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void A_borrowed_stream_is_left_open()
    {
        // Ownership is a decision the caller makes and the callee must respect.
        // Disposing a stream you were only lent breaks the caller who is still
        // writing to it -- and it is the caller who gets the exception.
        var output = new MemoryStream();
        var writer = new ShipmentLabelWriter(output, leaveOpen: true);

        writer.Write("SHIP-3");
        writer.Dispose();

        output.CanWrite.ShouldBeTrue();
        output.Dispose();
    }

    [Fact]
    public void Disposing_twice_is_harmless_for_a_borrowed_stream()
    {
        using var output = new MemoryStream();
        var writer = new ShipmentLabelWriter(output, leaveOpen: true);

        writer.Dispose();
        Should.NotThrow(writer.Dispose);

        output.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Writing_after_disposal_throws()
    {
        using var output = new MemoryStream();
        var writer = new ShipmentLabelWriter(output, leaveOpen: true);
        writer.Dispose();

        Should.Throw<ObjectDisposedException>(() => writer.Write("SHIP-4"));
    }

    [Fact]
    public void Everything_written_is_flushed_by_disposal()
    {
        var output = new MemoryStream();

        using (var writer = new ShipmentLabelWriter(output, leaveOpen: true))
        {
            writer.Write("SHIP-5");
            writer.Write("SHIP-6");
        }

        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("SHIP-5\nSHIP-6\n");
        output.Dispose();
    }
}
