using System.Data;
using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.Tests.Core;

public sealed class IsolationLevelsTests
{
    [Fact]
    public void Serializable_permits_nothing()
    {
        IsolationLevels.PermittedByStandard(IsolationLevel.Serializable).ShouldBeEmpty();
    }

    [Fact]
    public void Repeatable_read_still_permits_phantoms()
    {
        // The one people are surprised by: rows that appear are a different
        // problem from rows that change.
        IsolationLevels.PermittedByStandard(IsolationLevel.RepeatableRead)
            .ShouldBe([Anomaly.PhantomRead]);
    }

    [Fact]
    public void Read_committed_permits_everything_except_dirty_reads()
    {
        IsolationLevels.PermittedByStandard(IsolationLevel.ReadCommitted)
            .ShouldBe([Anomaly.NonRepeatableRead, Anomaly.PhantomRead], ignoreOrder: true);
    }

    [Fact]
    public void Read_uncommitted_permits_all_three()
    {
        IsolationLevels.PermittedByStandard(IsolationLevel.ReadUncommitted).Count.ShouldBe(3);
    }

    [Fact]
    public void Each_level_permits_a_subset_of_the_one_below_it()
    {
        var uncommitted = IsolationLevels.PermittedByStandard(IsolationLevel.ReadUncommitted);
        var committed = IsolationLevels.PermittedByStandard(IsolationLevel.ReadCommitted);
        var repeatable = IsolationLevels.PermittedByStandard(IsolationLevel.RepeatableRead);

        committed.ShouldBeSubsetOf(uncommitted);
        repeatable.ShouldBeSubsetOf(committed);
    }

    [Fact]
    public void A_level_outside_the_standard_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => IsolationLevels.PermittedByStandard(IsolationLevel.Chaos));
    }

    [Fact]
    public void PostgreSQL_silently_upgrades_read_uncommitted()
    {
        // Not an error, not a warning. Code written expecting cheap dirty reads
        // gets the semantics and the cost of the stricter level.
        IsolationLevels.EffectiveInPostgres(IsolationLevel.ReadUncommitted)
            .ShouldBe(IsolationLevel.ReadCommitted);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    public void And_honours_everything_else_as_asked(IsolationLevel level)
    {
        IsolationLevels.EffectiveInPostgres(level).ShouldBe(level);
    }
}
