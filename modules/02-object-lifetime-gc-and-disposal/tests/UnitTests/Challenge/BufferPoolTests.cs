using Shouldly;
using Training.Module02.Challenge;

namespace Training.Module02.Tests.Challenge;

public sealed class BufferPoolTests
{
    [Fact]
    public void Renting_gives_a_buffer_of_the_requested_size()
    {
        var pool = new BufferPool(bufferSize: 16);

        var buffer = pool.Rent();

        buffer.Length.ShouldBe(16);
        pool.Rented.ShouldBe(1);
    }

    [Fact]
    public void Two_rents_give_two_different_buffers()
    {
        var pool = new BufferPool(bufferSize: 8);

        var first = pool.Rent();
        var second = pool.Rent();

        ReferenceEquals(first, second).ShouldBeFalse();
        pool.Rented.ShouldBe(2);
    }

    [Fact]
    public void A_returned_buffer_is_handed_out_again()
    {
        var pool = new BufferPool(bufferSize: 8);

        var first = pool.Rent();
        pool.Return(first);

        pool.Available.ShouldBe(1);
        ReferenceEquals(pool.Rent(), first).ShouldBeTrue();
    }

    [Fact]
    public void Returning_clears_the_buffer_so_data_does_not_leak_between_callers()
    {
        var pool = new BufferPool(bufferSize: 4);

        var buffer = pool.Rent();
        buffer[0] = 42;
        pool.Return(buffer);

        pool.Rent()[0].ShouldBe((byte)0);
    }

    [Fact]
    public void Returning_the_same_buffer_twice_throws()
    {
        // Double-return is the pool equivalent of a double free: the buffer is
        // handed to two callers who both believe they own it.
        var pool = new BufferPool(bufferSize: 4);
        var buffer = pool.Rent();
        pool.Return(buffer);

        Should.Throw<InvalidOperationException>(() => pool.Return(buffer));
    }

    [Fact]
    public void Returning_a_buffer_this_pool_never_issued_throws()
    {
        var pool = new BufferPool(bufferSize: 4);

        Should.Throw<InvalidOperationException>(() => pool.Return(new byte[4]));
    }
}
