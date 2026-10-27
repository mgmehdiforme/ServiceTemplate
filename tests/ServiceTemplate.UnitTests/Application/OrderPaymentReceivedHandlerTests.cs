using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Orders.ProcessPayment;
using ServiceTemplate.Contracts.Events;
using ServiceTemplate.Domain.Exceptions;
using ServiceTemplate.Domain.Orders;
using ServiceTemplate.Domain.Orders.ValueObjects;
using ServiceTemplate.UnitTests.Common;
using Xunit;

namespace ServiceTemplate.UnitTests.Application;

public class OrderPaymentReceivedHandlerTests
{
    private readonly TestApplicationDbContext _dbContext = TestApplicationDbContext.CreateInMemory();
    private readonly ISemanticSearchService _semanticSearchService = Substitute.For<ISemanticSearchService>();

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ShouldThrowOrderNotFoundException()
    {
        // Arrange
        var handler = new OrderPaymentReceivedHandler(
            _dbContext,
            _semanticSearchService,
            NullLogger<OrderPaymentReceivedHandler>.Instance);

        var message = new OrderPaymentReceived(Guid.NewGuid(), "tx_123", 100m, "USD", DateTimeOffset.UtcNow);

        // Act
        Func<Task> act = async () => await handler.Handle(message, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OrderNotFoundException>();
        await _semanticSearchService.DidNotReceiveWithAnyArgs().IndexOrderAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_WhenOrderIsPending_ShouldMarkAsPaidAndIndexForSemanticSearch()
    {
        // Arrange
        var order = Order.Create(
            "cust_1",
            new Address("Street", "City", "Country"),
            [new OrderItem(Guid.NewGuid(), Guid.Empty, "SKU-1", "Headphones", 1, new Money(99m, "USD"))],
            "USD"
        );
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new OrderPaymentReceivedHandler(
            _dbContext,
            _semanticSearchService,
            NullLogger<OrderPaymentReceivedHandler>.Instance);

        var message = new OrderPaymentReceived(order.Id, "tx_123", 99m, "USD", DateTimeOffset.UtcNow);

        // Act
        await handler.Handle(message, CancellationToken.None);

        // Assert
        var updatedOrder = await _dbContext.Orders.FirstAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.Paid);

        await _semanticSearchService.Received(1).IndexOrderAsync(
            order.Id,
            Arg.Is<string>(c => c.Contains(order.Id.ToString()) && c.Contains("Headphones")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_WhenOrderIsAlreadyPaid_ShouldSkipProcessingIdempotently()
    {
        // Arrange
        var order = Order.Create(
            "cust_1",
            new Address("Street", "City", "Country"),
            [new OrderItem(Guid.NewGuid(), Guid.Empty, "SKU-1", "Headphones", 1, new Money(99m, "USD"))],
            "USD"
        );
        order.MarkAsPaid();
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new OrderPaymentReceivedHandler(
            _dbContext,
            _semanticSearchService,
            NullLogger<OrderPaymentReceivedHandler>.Instance);

        var message = new OrderPaymentReceived(order.Id, "tx_123", 99m, "USD", DateTimeOffset.UtcNow);

        // Act
        await handler.Handle(message, CancellationToken.None);

        // Assert: No new indexing should be requested for duplicate delivery
        await _semanticSearchService.DidNotReceiveWithAnyArgs().IndexOrderAsync(default, default!, default);
    }
}
