namespace Training.Module04.Core;

/// <summary>
/// Decides what happens to an order's money.
///
/// Exercise: express this as a switch expression over patterns rather than a
/// ladder of ifs. Arm order is behaviour, not layout — put the "not paid" arm
/// below the amount checks and a large unpaid order gets held for review
/// instead of being rejected outright.
///
/// Rules: an order that is not Paid is "nothing to settle", whatever else is
/// true of it. A negative amount is a "refund". Zero is "nothing to settle".
/// Otherwise a domestic order is "settle now"; a cross-border order of 1,000 or
/// more is "hold for review"; any other cross-border order is "settle in 3 days".
/// </summary>
public static class SettlementRules
{
    public static string Describe(Order order, string domesticRegion)
        => throw new NotImplementedException();
}
