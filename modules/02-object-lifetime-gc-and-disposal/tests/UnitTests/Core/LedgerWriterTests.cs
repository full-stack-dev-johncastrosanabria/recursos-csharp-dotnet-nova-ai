using Shouldly;
using Training.Module02.Core;

namespace Training.Module02.Tests.Core;

public sealed class LedgerWriterTests
{
    [Fact]
    public async Task Entries_are_buffered_until_the_threshold()
    {
        var sink = new List<string>();
        await using var writer = new LedgerWriter(sink, flushThreshold: 3);

        await writer.WriteAsync("debit 10");
        await writer.WriteAsync("credit 10");

        sink.ShouldBeEmpty();
        writer.Pending.ShouldBe(2);
    }

    [Fact]
    public async Task Reaching_the_threshold_flushes()
    {
        var sink = new List<string>();
        await using var writer = new LedgerWriter(sink, flushThreshold: 2);

        await writer.WriteAsync("debit 10");
        await writer.WriteAsync("credit 10");

        sink.ShouldBe(["debit 10", "credit 10"]);
        writer.Pending.ShouldBe(0);
    }

    [Fact]
    public async Task Disposing_flushes_what_is_left()
    {
        // The entries below never reach the threshold. Without a flush in
        // DisposeAsync they are simply lost, and nothing reports it.
        var sink = new List<string>();
        var writer = new LedgerWriter(sink, flushThreshold: 100);

        await writer.WriteAsync("debit 10");
        await writer.DisposeAsync();

        sink.ShouldBe(["debit 10"]);
    }

    [Fact]
    public async Task An_await_using_block_flushes_at_the_end_of_scope()
    {
        var sink = new List<string>();

        await using (var writer = new LedgerWriter(sink, flushThreshold: 100))
        {
            await writer.WriteAsync("credit 5");
            sink.ShouldBeEmpty();
        }

        sink.ShouldBe(["credit 5"]);
    }

    [Fact]
    public async Task Disposing_twice_does_not_write_the_entries_twice()
    {
        var sink = new List<string>();
        var writer = new LedgerWriter(sink, flushThreshold: 100);

        await writer.WriteAsync("debit 1");
        await writer.DisposeAsync();
        await writer.DisposeAsync();

        sink.ShouldBe(["debit 1"]);
    }

    [Fact]
    public async Task Writing_after_disposal_throws()
    {
        var sink = new List<string>();
        var writer = new LedgerWriter(sink, flushThreshold: 100);
        await writer.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await writer.WriteAsync("debit 1"));
    }
}
