using ServiceTemplate.Contracts.Common;

namespace ServiceTemplate.Contracts.Events;

/// <summary>
/// Public integration event dispatched via RabbitMQ when an order is created.
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt
) : IntegrationEvent;
