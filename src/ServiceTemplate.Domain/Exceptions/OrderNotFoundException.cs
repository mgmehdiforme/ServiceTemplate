namespace ServiceTemplate.Domain.Exceptions;

/// <summary>
/// Exception thrown when an expected Order aggregate is not found.
/// </summary>
public class OrderNotFoundException : DomainException
{
    public Guid OrderId { get; }

    public OrderNotFoundException(Guid orderId)
        : base($"Order with identifier '{orderId}' was not found.")
    {
        OrderId = orderId;
    }
}
