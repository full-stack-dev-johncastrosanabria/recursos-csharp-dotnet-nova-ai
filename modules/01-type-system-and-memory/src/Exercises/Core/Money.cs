namespace Training.Module01.Core;

/// <summary>Raised when two amounts in different currencies are combined.</summary>
public sealed class CurrencyMismatchException(string message) : InvalidOperationException(message);

/// <summary>
/// An amount of money in a single currency.
///
/// Exercise: make this a value with correct equality. Two instances holding the
/// same amount and the same currency must be equal, must hash the same, and must
/// work as a dictionary key. Adding across currencies must be refused, not guessed.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => throw new NotImplementedException();

    public Money Add(Money other) => throw new NotImplementedException();

    public Money Multiply(int quantity) => throw new NotImplementedException();
}
