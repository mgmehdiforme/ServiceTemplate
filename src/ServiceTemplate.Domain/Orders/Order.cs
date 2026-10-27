using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Exceptions;
using ServiceTemplate.Domain.Orders.Events;
using ServiceTemplate.Domain.Orders.ValueObjects;

namespace ServiceTemplate.Domain.Orders;

/// <summary>
/// Order Aggregate Root encapsulating customer, items, total calculation, and status invariants.
/// </summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public string CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Private parameterless constructor for EF Core materialization
    private Order() : base()
    {
        CustomerId = string.Empty;
        Status = OrderStatus.Pending;
        ShippingAddress = null!;
        TotalAmount = Money.Zero();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Order(
        Guid id,
        string customerId,
        Address shippingAddress,
        List<OrderItem> items,
        string currency)
        : base(id)
    {
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;

        decimal totalSum = 0m;
        foreach (var item in items)
        {
            item.AssignToOrder(id);
            _items.Add(item);
            totalSum += item.Total.Amount;
        }

        TotalAmount = new Money(totalSum, currency);

        AddDomainEvent(new OrderSubmittedDomainEvent(Id, CustomerId, TotalAmount.Amount, TotalAmount.Currency));
    }

    /// <summary>
    /// Creates and validates a new Order aggregate.
    /// </summary>
    public static Order Create(
        string customerId,
        Address shippingAddress,
        IEnumerable<OrderItem> items,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainException("Customer identifier cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(shippingAddress);
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new DomainException("An order must contain at least one line item.");
        }

        var orderId = Guid.NewGuid();
        return new Order(orderId, customerId.Trim(), shippingAddress, itemList, currency);
    }

    /// <summary>
    /// Transitions order status to Paid upon successful payment receipt.
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Cannot mark order '{Id}' as paid because its current status is '{Status}'. Only Pending orders can be marked as paid.");
        }

        Status = OrderStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderPaidDomainEvent(Id, UpdatedAt.Value));
    }

    /// <summary>
    /// Cancels the order if not already shipped.
    /// </summary>
    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A cancellation reason must be provided.");
        }

        if (Status == OrderStatus.Shipped)
        {
            throw new DomainException($"Cannot cancel order '{Id}' because it has already been shipped.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
