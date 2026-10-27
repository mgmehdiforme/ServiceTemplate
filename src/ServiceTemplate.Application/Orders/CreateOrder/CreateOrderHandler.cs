using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Contracts.Events;
using ServiceTemplate.Domain.Orders;
using ServiceTemplate.Domain.Orders.ValueObjects;
using Wolverine;

namespace ServiceTemplate.Application.Orders.CreateOrder;

/// <summary>
/// Wolverine message handler orchestrating order aggregate creation and transactional outbox event dispatch.
/// </summary>
public class CreateOrderHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMessageBus _bus;

    public CreateOrderHandler(IApplicationDbContext dbContext, IMessageBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var address = new Address(
            command.ShippingAddress.Street,
            command.ShippingAddress.City,
            command.ShippingAddress.Country);

        var items = command.Items.Select(i => new OrderItem(
            Guid.NewGuid(),
            Guid.Empty,
            i.Sku,
            i.ProductName,
            i.Quantity,
            new Money(i.UnitPrice, command.Currency)
        ));

        var order = Order.Create(command.CustomerId, address, items, command.Currency);
        _dbContext.Orders.Add(order);

        // Enlist integration event in Wolverine transactional outbox
        var integrationEvent = new OrderCreatedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt
        );
        await _bus.PublishAsync(integrationEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.CustomerId,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Status.ToString()
        );
    }
}
