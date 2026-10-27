using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Orders.Events;

/// <summary>
/// Domain event published when a new Order is created and submitted.
/// </summary>
public sealed record OrderSubmittedDomainEvent(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredOn
) : IDomainEvent
{
    public OrderSubmittedDomainEvent(Guid orderId, string customerId, decimal totalAmount, string currency)
        : this(orderId, customerId, totalAmount, currency, DateTimeOffset.UtcNow)
    {
    }
}
