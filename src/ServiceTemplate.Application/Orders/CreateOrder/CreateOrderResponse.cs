namespace ServiceTemplate.Application.Orders.CreateOrder;

/// <summary>
/// Response payload returned after successfully creating an order.
/// </summary>
public sealed record CreateOrderResponse(
    Guid Id,
    string CustomerId,
    decimal TotalAmount,
    string Currency,
    string Status
);
