namespace Training.Module04.Core;

/// <summary>
/// Decides what happens to an order's money.
///
/// Read the arms top to bottom: that is the order they are tested in, and it is
/// behaviour rather than layout. The "not paid" arm has to come first, or a
/// large unpaid order matches the cross-border amount check below it and gets
/// held for review instead of being rejected outright. A switch expression makes
/// that ordering visible in a way a ladder of ifs with early returns does not.
///
/// The compiler also checks the arms are reachable and the result is exhaustive,
/// which is why the discard at the end is the only default.
/// </summary>
public static class SettlementRules
{
    public static string Describe(Order order, string domesticRegion)
        => order switch
        {
            { State: not OrderState.Paid } => "nothing to settle",
            { Amount: < 0 } => "refund",
            { Amount: 0 } => "nothing to settle",
            var o when string.Equals(o.Region, domesticRegion, StringComparison.Ordinal)
                => "settle now",
            { Amount: >= 1000m } => "hold for review",
            _ => "settle in 3 days",
        };
}
