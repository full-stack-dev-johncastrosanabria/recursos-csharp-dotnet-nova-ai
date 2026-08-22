namespace Training.Module01.Core;

/// <summary>Raised when two amounts in different currencies are combined.</summary>
public sealed class CurrencyMismatchException(string message) : InvalidOperationException(message);

/// <summary>
/// An amount of money in a single currency.
///
/// `readonly record struct` gives value equality, a matching GetHashCode and a
/// sensible ToString for free, with no heap allocation. Writing all of that by
/// hand is the exercise in CustomerReference; here the point is knowing when
/// the compiler will do it correctly for you.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(
                $"Cannot add {other.Currency} to {Currency}. Convert first, explicitly.");
        }

        return this with { Amount = Amount + other.Amount };
    }

    public Money Multiply(int quantity) => this with { Amount = Amount * quantity };
}
