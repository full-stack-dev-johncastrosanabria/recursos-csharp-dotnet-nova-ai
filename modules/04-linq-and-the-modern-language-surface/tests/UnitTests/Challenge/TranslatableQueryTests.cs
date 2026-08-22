using System.Linq.Expressions;
using Shouldly;
using Training.Module04.Challenge;
using Training.Module04.Core;

namespace Training.Module04.Tests.Challenge;

public sealed class TranslatableQueryTests
{
    private static readonly Order[] Orders =
    [
        new("ord_1", "EU", 120m, OrderState.Paid),
        new("ord_2", "EU", 40m, OrderState.Pending),
        new("ord_3", "US", 300m, OrderState.Paid),
        new("ord_4", "US", 15m, OrderState.Cancelled),
    ];

    /// <summary>
    /// Counts calls to a named LINQ operator inside an expression tree. What a
    /// provider can translate is exactly what reached the tree; anything the
    /// query did client-side is absent from it.
    /// </summary>
    private sealed class OperatorCounter(string name) : ExpressionVisitor
    {
        public int Count { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == name)
            {
                Count++;
            }

            return base.VisitMethodCall(node);
        }
    }

    private static int CountOperator(Expression expression, string name)
    {
        var counter = new OperatorCounter(name);
        counter.Visit(expression);
        return counter.Count;
    }

    [Fact]
    public void Returns_the_right_orders()
    {
        var found = TranslatableQuery.HighValuePaid(Orders.AsQueryable(), 100m).ToArray();

        found.Select(o => o.Id).ShouldBe(["ord_1", "ord_3"]);
    }

    [Fact]
    public void The_filter_reaches_the_expression_tree()
    {
        // This is the module's real-world case, as an assertion. Both the good
        // and the bad implementation return identical results here; only the
        // tree says which one the database will actually run.
        var query = TranslatableQuery.HighValuePaid(Orders.AsQueryable(), 100m);

        CountOperator(query.Expression, "Where").ShouldBeGreaterThan(0);
    }

    [Fact]
    public void The_query_never_falls_back_to_client_evaluation()
    {
        var query = TranslatableQuery.HighValuePaid(Orders.AsQueryable(), 100m);

        CountOperator(query.Expression, "AsEnumerable").ShouldBe(0);
        CountOperator(query.Expression, "ToList").ShouldBe(0);
        CountOperator(query.Expression, "ToArray").ShouldBe(0);
    }

    [Fact]
    public void It_stays_an_IQueryable_so_the_caller_can_keep_composing()
    {
        var query = TranslatableQuery.HighValuePaid(Orders.AsQueryable(), 100m);

        var narrowed = query.Where(o => o.Region == "EU");

        narrowed.Select(o => o.Id).ShouldBe(["ord_1"]);
        CountOperator(narrowed.Expression, "Where").ShouldBe(2);
    }

    [Fact]
    public void The_settleable_rule_is_an_expression_rather_than_a_delegate()
    {
        // A Func<Order, bool> cannot be translated: the provider sees an opaque
        // delegate and must fetch every row to run it. Returning an Expression
        // is what keeps a shared rule usable server-side.
        var rule = TranslatableQuery.IsSettleable("EU");

        // The point is not the declared type -- that is guaranteed by the
        // signature. It is that the body is a tree a provider can read. A
        // compiled Func would be opaque, so the provider's only option would be
        // to fetch every row and filter locally.
        rule.Parameters.Count.ShouldBe(1);
        rule.Body.ShouldBeAssignableTo<BinaryExpression>();
        rule.ToString().ShouldContain("State");
    }

    [Fact]
    public void The_settleable_rule_composes_into_a_query_and_still_translates()
    {
        var rule = TranslatableQuery.IsSettleable("EU");

        var query = Orders.AsQueryable().Where(rule);

        query.Select(o => o.Id).ShouldBe(["ord_1"]);
        CountOperator(query.Expression, "Where").ShouldBe(1);
    }

    [Fact]
    public void The_rule_agrees_with_itself_when_run_in_memory()
    {
        var rule = TranslatableQuery.IsSettleable("EU");
        var compiled = rule.Compile();

        Orders.Where(compiled).Select(o => o.Id).ShouldBe(["ord_1"]);
    }
}
