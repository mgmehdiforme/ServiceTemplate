using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Exceptions;

namespace ServiceTemplate.Domain.Orders.ValueObjects;

/// <summary>
/// Value object representing a shipping or physical address.
/// </summary>
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }

    public Address(string street, string city, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("Street cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("City cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException("Country cannot be empty.");
        }

        Street = street.Trim();
        City = city.Trim();
        Country = country.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street.ToLowerInvariant();
        yield return City.ToLowerInvariant();
        yield return Country.ToLowerInvariant();
    }

    public override string ToString() => $"{Street}, {City}, {Country}";
}
