using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Orders.Events;

/// <summary>
/// Domain event published when payment for an Order has been received.
/// </summary>
public sealed record OrderPaidDomainEvent(
    Guid OrderId,
    DateTimeOffset PaidAt,
    DateTimeOffset OccurredOn
) : IDomainEvent
{
    public OrderPaidDomainEvent(Guid orderId, DateTimeOffset paidAt)
        : this(orderId, paidAt, DateTimeOffset.UtcNow)
    {
    }
}
