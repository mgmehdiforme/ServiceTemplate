using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Exceptions;
using ServiceTemplate.Domain.Orders.ValueObjects;

namespace ServiceTemplate.Domain.Orders;

/// <summary>
/// Child line-item entity within an Order aggregate.
/// </summary>
public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public string Sku { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money Total => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    // Private constructor for EF Core materialization
    private OrderItem() : base()
    {
        Sku = string.Empty;
        ProductName = string.Empty;
        UnitPrice = Money.Zero();
    }

    public OrderItem(Guid id, Guid orderId, string sku, string productName, int quantity, Money unitPrice)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Item SKU cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Item product name cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException($"Item quantity must be greater than zero. Value: {quantity}");
        }

        ArgumentNullException.ThrowIfNull(unitPrice);

        OrderId = orderId;
        Sku = sku.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void AssignToOrder(Guid orderId)
    {
        OrderId = orderId;
    }
}
