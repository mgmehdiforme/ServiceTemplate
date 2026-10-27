namespace ServiceTemplate.Domain.Orders;

/// <summary>
/// Lifecycle status of an Order aggregate.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Cancelled = 3,
    Shipped = 4
}
