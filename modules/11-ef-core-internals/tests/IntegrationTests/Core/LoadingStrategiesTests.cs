using Microsoft.EntityFrameworkCore;
using Shouldly;
using Training.Module11.Core;

namespace Training.Module11.IntegrationTests.Core;

[Collection(SharedShopDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class LoadingStrategiesTests(ShopDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Lazy_loading_in_a_loop_is_one_query_per_order()
    {
        // The module's real-world case, counted. Fifty orders, fifty-one
        // commands, and nothing in the C# says "query" except the ToList.
        await using var db = database.Create(lazyLoading: true);
        database.Commands.Reset();

        var lines = 0;
        foreach (var order in await LoadingStrategies.WithoutLines(db).OrderBy(o => o.Id).Take(50).ToListAsync(Token))
        {
            lines += order.Lines.Count;
        }

        lines.ShouldBe(150);
        database.Commands.Count.ShouldBe(51);
    }

    [Fact]
    public async Task Include_gets_the_same_data_in_one()
    {
        await using var db = database.Create();
        database.Commands.Reset();

        var orders = await LoadingStrategies.Eager(db).OrderBy(o => o.Id).Take(50).ToListAsync(Token);

        orders.Sum(order => order.Lines.Count).ShouldBe(150);
        database.Commands.Count.ShouldBe(1);
    }

    [Fact]
    public async Task So_does_a_projection_and_it_returns_less()
    {
        await using var db = database.Create();
        database.Commands.Reset();

        var summaries = await LoadingStrategies.Projected(db).Take(50).ToListAsync(Token);

        summaries.Count.ShouldBe(50);
        summaries.Sum(summary => summary.LineCount).ShouldBe(150);
        database.Commands.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_projection_tracks_nothing()
    {
        await using var db = database.Create();

        await LoadingStrategies.Projected(db).Take(50).ToListAsync(Token);

        TrackingBehaviour.TrackedCount(db).ShouldBe(0);
    }

    [Fact]
    public async Task Include_tracks_every_entity_it_materialised()
    {
        await using var db = database.Create();

        await LoadingStrategies.Eager(db).OrderBy(o => o.Id).Take(50).ToListAsync(Token);

        // 50 orders and 150 lines, all held until the context is disposed.
        TrackingBehaviour.TrackedCount(db).ShouldBe(200);
    }
}
