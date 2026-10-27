using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ServiceTemplate.Application.Orders.CreateOrder;
using ServiceTemplate.Contracts.Events;
using ServiceTemplate.UnitTests.Common;
using Wolverine;
using Xunit;

namespace ServiceTemplate.UnitTests.Application;

public class CreateOrderHandlerTests
{
    private readonly TestApplicationDbContext _dbContext = TestApplicationDbContext.CreateInMemory();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistOrderAndPublishOutboxEvent()
    {
        // Arrange
        var handler = new CreateOrderHandler(_dbContext, _bus);
        var command = new CreateOrderCommand(
            CustomerId: "cust_999",
            Items:
            [
                new CreateOrderItemDto("SKU-1", "Gaming Mouse", 2, 49.99m),
                new CreateOrderItemDto("SKU-2", "Mechanical Keyboard", 1, 129.99m)
            ],
            ShippingAddress: new ShippingAddressDto("10 Downing St", "London", "UK"),
            Currency: "USD"
        );

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.CustomerId.Should().Be("cust_999");
        response.TotalAmount.Should().Be(229.97m);
        response.Currency.Should().Be("USD");
        response.Status.Should().Be("Pending");

        // Verify database persistence
        var persistedOrder = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == response.Id);
        persistedOrder.Should().NotBeNull();
        persistedOrder!.CustomerId.Should().Be("cust_999");
        persistedOrder.Items.Should().HaveCount(2);

        // Verify outbox publication via Wolverine
        await _bus.Received(1).PublishAsync(Arg.Is<OrderCreatedIntegrationEvent>(e =>
            e.OrderId == response.Id &&
            e.CustomerId == "cust_999" &&
            e.TotalAmount == 229.97m &&
            e.Currency == "USD"
        ));
    }
}
