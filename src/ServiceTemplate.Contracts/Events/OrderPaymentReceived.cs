using ServiceTemplate.Contracts.Common;

namespace ServiceTemplate.Contracts.Events;

/// <summary>
/// Public integration event received from the payment gateway / finance microservice.
/// </summary>
public sealed record OrderPaymentReceived(
    Guid OrderId,
    string PaymentTransactionId,
    decimal AmountPaid,
    string Currency,
    DateTimeOffset PaidAt
) : IntegrationEvent;
