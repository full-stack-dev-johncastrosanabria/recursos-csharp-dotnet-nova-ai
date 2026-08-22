// The module's real-world case, reproduced. This is not a story about something
// that happened to someone: run it and watch the same customer get charged
// twice, then run the fix and watch the retry be recognised.
//
// The mechanism is one line of the language doing exactly what it promises.

using System.Globalization;

Console.WriteLine("A checkout is retried. Does the idempotency cache recognise it?");
Console.WriteLine();

var firstAttempt = Basket();
var retry = Basket();

Console.WriteLine("The two attempts are separate objects holding identical data:");
Console.WriteLine($"  same instance?           {ReferenceEquals(firstAttempt, retry)}");
Console.WriteLine($"  same customer and lines? {firstAttempt.CustomerId == retry.CustomerId} and {firstAttempt.Lines.SequenceEqual(retry.Lines)}");
Console.WriteLine();

Console.WriteLine("--- With the key as a plain record ---");
var brokenCache = new Dictionary<BrokenBasketKey, string>
{
    [new BrokenBasketKey(firstAttempt.CustomerId, firstAttempt.Lines)] = "charge_001",
};

var brokenKey = new BrokenBasketKey(retry.CustomerId, retry.Lines);
var brokenHit = brokenCache.TryGetValue(brokenKey, out var brokenCharge);

Console.WriteLine($"  keys compare equal?  {brokenCache.Keys.First() == brokenKey}");
Console.WriteLine($"  hashes match?        {brokenCache.Keys.First().GetHashCode() == brokenKey.GetHashCode()}");
Console.WriteLine($"  cache hit?           {brokenHit}");
Console.WriteLine(brokenHit
    ? $"  -> reuses {brokenCharge}"
    : "  -> MISS. A second charge is issued. The customer is billed twice.");
Console.WriteLine();

Console.WriteLine("--- With Equals and GetHashCode written to match ---");
var fixedCache = new Dictionary<FixedBasketKey, string>
{
    [new FixedBasketKey(firstAttempt.CustomerId, firstAttempt.Lines)] = "charge_001",
};

var fixedKey = new FixedBasketKey(retry.CustomerId, retry.Lines);
var fixedHit = fixedCache.TryGetValue(fixedKey, out var fixedCharge);

Console.WriteLine($"  keys compare equal?  {fixedCache.Keys.First() == fixedKey}");
Console.WriteLine($"  hashes match?        {fixedCache.Keys.First().GetHashCode() == fixedKey.GetHashCode()}");
Console.WriteLine($"  cache hit?           {fixedHit}");
Console.WriteLine(fixedHit
    ? $"  -> reuses {fixedCharge}. The retry is recognised. No second charge."
    : "  -> MISS.");
Console.WriteLine();

Console.WriteLine("Why the record was wrong: Lines is an IReadOnlyList<LineItem>, a reference.");
Console.WriteLine("Compiler-generated record equality compares it with the default comparer,");
Console.WriteLine("which for a list is reference equality. Two identical baskets are therefore");
Console.WriteLine("unequal, hash differently, and land in different buckets.");
Console.WriteLine();

Console.WriteLine("--- What it costs ---");
Console.WriteLine();
Console.WriteLine("These are assumptions, not measurements. Substitute your own figures --");
Console.WriteLine("the point is the shape of the arithmetic, not this particular total.");
Console.WriteLine();

const int OrdersPerDay = 4_000;
const double RetryRate = 0.02;
const decimal AverageBasket = 85m;
const decimal ChargebackFee = 15m;

var duplicatesPerDay = OrdersPerDay * RetryRate;
var costPerDuplicate = AverageBasket + ChargebackFee;
var perDay = (decimal)duplicatesPerDay * costPerDuplicate;

Console.WriteLine($"  orders per day                 {OrdersPerDay.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  share retried                  {RetryRate.ToString("P1", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  duplicate charges per day      {duplicatesPerDay.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  average basket (USD)           {AverageBasket.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  chargeback fee per dispute     {ChargebackFee.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  cost per duplicate             {costPerDuplicate.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  cost per day                   {perDay.ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine($"  cost per 30 days               {(perDay * 30).ToString("N0", CultureInfo.InvariantCulture),12}");
Console.WriteLine();
Console.WriteLine("The detection gap is the expensive part. Nothing throws, no error is logged,");
Console.WriteLine("and every dashboard stays green: the system is successfully doing the wrong");
Console.WriteLine("thing. It surfaces as customer complaints, days later, one at a time.");

static Checkout Basket() => new("cus_17", [new LineItem("SKU-1", 2), new LineItem("SKU-2", 1)]);

internal readonly record struct LineItem(string Sku, int Quantity);

internal sealed record Checkout(string CustomerId, IReadOnlyList<LineItem> Lines);

/// <summary>Value equality from the compiler, which compares Lines by reference.</summary>
internal sealed record BrokenBasketKey(string CustomerId, IReadOnlyList<LineItem> Lines);

/// <summary>The same key with Equals and GetHashCode agreeing on structure.</summary>
internal sealed record FixedBasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)
{
    public bool Equals(FixedBasketKey? other)
        => other is not null
           && string.Equals(CustomerId, other.CustomerId, StringComparison.Ordinal)
           && Lines.SequenceEqual(other.Lines);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CustomerId, StringComparer.Ordinal);

        foreach (var line in Lines)
        {
            hash.Add(line);
        }

        return hash.ToHashCode();
    }
}
