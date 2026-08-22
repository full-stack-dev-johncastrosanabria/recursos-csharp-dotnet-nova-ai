using System.Linq.Expressions;
using Training.Module04.Core;

namespace Training.Module04.Challenge;

/// <summary>
/// Queries that a database provider can actually translate.
///
/// Nothing here looks clever, and that is the lesson. The difference between
/// this and the version that brings the whole table into memory is a single
/// call — `.AsEnumerable()`, `.ToList()`, or a helper typed
/// Func&lt;Order, bool&gt; instead of Expression&lt;Func&lt;Order, bool&gt;&gt;.
/// Both versions return identical results, so no test that only checks results
/// can tell them apart.
///
/// Returning IQueryable rather than IEnumerable is what keeps the decision in
/// the caller's hands: they can add another Where, an OrderBy or a Skip, and
/// all of it still reaches the provider as one query.
///
/// IsSettleable returns an Expression because a compiled delegate is opaque.
/// The provider cannot see inside a Func, so its only option is to fetch every
/// row and run it locally — which is exactly the failure this module is about,
/// arriving through a helper that looked like good factoring.
/// </summary>
public static class TranslatableQuery
{
    public static IQueryable<Order> HighValuePaid(IQueryable<Order> orders, decimal threshold)
        => orders.Where(order => order.State == OrderState.Paid && order.Amount > threshold);

    public static Expression<Func<Order, bool>> IsSettleable(string domesticRegion)
        => order => order.State == OrderState.Paid && order.Region == domesticRegion;
}
