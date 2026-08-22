using System.Linq.Expressions;
using Training.Module04.Core;

namespace Training.Module04.Challenge;

/// <summary>
/// Queries that a database provider can actually translate.
///
/// Challenge: this is the module's real-world case. An IQueryable carries an
/// expression tree that the provider turns into SQL. The moment anything forces
/// it back to IEnumerable — AsEnumerable, ToList, or a method that takes
/// Func&lt;Order, bool&gt; instead of an expression — the rest of the query runs in
/// your process, over every row the database was willing to send.
///
/// The results are identical either way, which is why unit tests against an
/// in-memory list never notice. Only the shape of the expression tree says
/// which one the database will run.
///
/// IsSettleable must return an Expression, not a delegate. A compiled Func is
/// opaque to the provider: it cannot look inside, so it fetches everything and
/// filters afterwards.
/// </summary>
public static class TranslatableQuery
{
    public static IQueryable<Order> HighValuePaid(IQueryable<Order> orders, decimal threshold)
        => throw new NotImplementedException();

    public static Expression<Func<Order, bool>> IsSettleable(string domesticRegion)
        => throw new NotImplementedException();
}
