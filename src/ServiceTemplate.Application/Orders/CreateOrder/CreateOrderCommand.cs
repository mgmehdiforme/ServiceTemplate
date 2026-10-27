namespace ServiceTemplate.Application.Orders.CreateOrder;

/// <summary>
/// Command to create and submit a new customer order.
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerId,
    IReadOnlyList<CreateOrderItemDto> Items,
    ShippingAddressDto ShippingAddress,
    string Currency = "USD"
);

public sealed record CreateOrderItemDto(
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed record ShippingAddressDto(
    string Street,
    string City,
    string Country
);
