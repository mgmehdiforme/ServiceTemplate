using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Exceptions;

namespace ServiceTemplate.Domain.Orders.ValueObjects;

/// <summary>
/// Monetary value object enforcing currency consistency and non-negative balances.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException($"Monetary amount cannot be negative. Value: {amount}");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new DomainException($"Currency code must be a valid 3-letter ISO code. Value: '{currency}'");
        }

        Amount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"Cannot add money of differing currencies: '{left.Currency}' and '{right.Currency}'.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"Cannot subtract money of differing currencies: '{left.Currency}' and '{right.Currency}'.");
        }

        if (left.Amount < right.Amount)
        {
            throw new DomainException($"Resulting money amount cannot be negative. Minuend: {left.Amount}, Subtrahend: {right.Amount}");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
