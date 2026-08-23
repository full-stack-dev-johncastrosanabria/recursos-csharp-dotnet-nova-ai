using System.Data;
using Shouldly;
using Training.Module10.Challenge;

namespace Training.Module10.Tests.Challenge;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task Work_that_succeeds_is_committed()
    {
        var source = new FakeSource();

        var result = await UnitOfWork.ExecuteAsync(source, IsolationLevel.ReadCommitted, () => Task.FromResult(7));

        result.ShouldBe(7);
        source.Transaction.Committed.ShouldBeTrue();
        source.Transaction.RolledBack.ShouldBeFalse();
    }

    [Fact]
    public async Task The_requested_isolation_level_is_the_one_used()
    {
        var source = new FakeSource();

        await UnitOfWork.ExecuteAsync(source, IsolationLevel.Serializable, () => Task.FromResult(0));

        source.Level.ShouldBe(IsolationLevel.Serializable);
    }

    [Fact]
    public async Task Work_that_throws_is_rolled_back()
    {
        var source = new FakeSource();

        await Should.ThrowAsync<InvalidOperationException>(
            () => UnitOfWork.ExecuteAsync<int>(source, IsolationLevel.ReadCommitted,
                () => throw new InvalidOperationException("no stock")));

        source.Transaction.RolledBack.ShouldBeTrue();
        source.Transaction.Committed.ShouldBeFalse();
    }

    [Fact]
    public async Task And_the_original_exception_reaches_the_caller()
    {
        // Turning a failed transaction into a returned default is how a caller
        // comes to believe something was saved.
        var source = new FakeSource();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => UnitOfWork.ExecuteAsync<int>(source, IsolationLevel.ReadCommitted,
                () => throw new InvalidOperationException("no stock")));

        thrown.Message.ShouldBe("no stock");
    }

    [Fact]
    public async Task The_transaction_is_disposed_either_way()
    {
        var committed = new FakeSource();
        await UnitOfWork.ExecuteAsync(committed, IsolationLevel.ReadCommitted, () => Task.FromResult(1));

        var failed = new FakeSource();
        await Should.ThrowAsync<InvalidOperationException>(
            () => UnitOfWork.ExecuteAsync<int>(failed, IsolationLevel.ReadCommitted,
                () => throw new InvalidOperationException("boom")));

        committed.Transaction.Disposed.ShouldBeTrue();
        failed.Transaction.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task A_failing_rollback_does_not_hide_why_you_were_rolling_back()
    {
        // The rollback failed because the connection is already gone. The
        // useful exception is still the first one.
        var source = new FakeSource { RollbackThrows = true };

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => UnitOfWork.ExecuteAsync<int>(source, IsolationLevel.ReadCommitted,
                () => throw new InvalidOperationException("no stock")));

        thrown.Message.ShouldBe("no stock");
    }

    private sealed class FakeSource : ITransactionSource
    {
        public bool RollbackThrows { get; init; }

        public IsolationLevel Level { get; private set; }

        public FakeTransaction Transaction { get; } = new();

        public Task<ITransaction> BeginAsync(IsolationLevel level)
        {
            Level = level;
            Transaction.RollbackThrows = RollbackThrows;

            return Task.FromResult<ITransaction>(Transaction);
        }
    }

    private sealed class FakeTransaction : ITransaction
    {
        public bool RollbackThrows { get; set; }

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public bool Disposed { get; private set; }

        public Task CommitAsync()
        {
            Committed = true;

            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            RolledBack = true;

            return RollbackThrows
                ? throw new TimeoutException("connection lost")
                : Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }
}
