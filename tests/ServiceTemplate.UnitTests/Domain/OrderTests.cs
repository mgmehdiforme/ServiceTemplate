using FluentAssertions;
using ServiceTemplate.Domain.Exceptions;
using ServiceTemplate.Domain.Orders;
using ServiceTemplate.Domain.Orders.Events;
using ServiceTemplate.Domain.Orders.ValueObjects;
using Xunit;

namespace ServiceTemplate.UnitTests.Domain;

public class OrderTests
{
    private static Address CreateValidAddress() => new("10 Downing St", "London", "UK");

    private static OrderItem CreateValidItem(string sku = "PROD-1", decimal price = 100m, int qty = 2)
        => new(Guid.NewGuid(), Guid.Empty, sku, "Mechanical Keyboard", qty, new Money(price, "USD"));

    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateOrderAndRaiseEvent()
    {
        // Arrange
        var customerId = "cust_12345";
        var address = CreateValidAddress();
        var item1 = CreateValidItem("PROD-1", 100m, 2);
        var item2 = CreateValidItem("PROD-2", 50m, 1);

        // Act
        var order = Order.Create(customerId, address, [item1, item2], "USD");

        // Assert
        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.ShippingAddress.Should().Be(address);
        order.TotalAmount.Amount.Should().Be(250m);
        order.TotalAmount.Currency.Should().Be("USD");
        order.Items.Should().HaveCount(2);

        order.DomainEvents.Should().ContainSingle(e => e is OrderSubmittedDomainEvent);
        var submittedEvent = order.DomainEvents.OfType<OrderSubmittedDomainEvent>().Single();
        submittedEvent.OrderId.Should().Be(order.Id);
        submittedEvent.CustomerId.Should().Be(customerId);
        submittedEvent.TotalAmount.Should().Be(250m);
        submittedEvent.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyCustomerId_ShouldThrowDomainException(string? invalidCustomerId)
    {
        // Arrange
        var address = CreateValidAddress();
        var item = CreateValidItem();

        // Act
        Action act = () => Order.Create(invalidCustomerId!, address, [item]);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Customer identifier cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyItems_ShouldThrowDomainException()
    {
        // Arrange
        var address = CreateValidAddress();

        // Act
        Action act = () => Order.Create("cust_1", address, []);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*at least one line item*");
    }

    [Fact]
    public void MarkAsPaid_WhenPending_ShouldTransitionToPaidAndRaiseEvent()
    {
        // Arrange
        var order = Order.Create("cust_1", CreateValidAddress(), [CreateValidItem()]);
        order.ClearDomainEvents();

        // Act
        order.MarkAsPaid();

        // Assert
        order.Status.Should().Be(OrderStatus.Paid);
        order.UpdatedAt.Should().NotBeNull();
        order.DomainEvents.Should().ContainSingle(e => e is OrderPaidDomainEvent);
        var paidEvent = order.DomainEvents.OfType<OrderPaidDomainEvent>().Single();
        paidEvent.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create("cust_1", CreateValidAddress(), [CreateValidItem()]);
        order.MarkAsPaid();

        // Act
        Action act = () => order.MarkAsPaid();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Only Pending orders can be marked as paid*");
    }

    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = Order.Create("cust_1", CreateValidAddress(), [CreateValidItem()]);

        // Act
        order.Cancel("Customer requested cancellation");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Money_OperationsAndValidation_ShouldEnforceInvariants()
    {
        // Valid creation & equality
        var m1 = new Money(100m, "usd");
        var m2 = new Money(100m, "USD");
        m1.Should().Be(m2);
        (m1 == m2).Should().BeTrue();

        // Addition
        var sum = m1 + new Money(50m, "USD");
        sum.Amount.Should().Be(150m);
        sum.Currency.Should().Be("USD");

        // Subtraction
        var diff = sum - new Money(50m, "USD");
        diff.Amount.Should().Be(100m);

        // Negative check
        Action actNegative = () => new Money(-1m, "USD");
        actNegative.Should().Throw<DomainException>();

        // Currency mismatch check
        Action actMismatch = () => _ = m1 + new Money(10m, "EUR");
        actMismatch.Should().Throw<DomainException>()
            .WithMessage("*differing currencies*");
    }

    [Fact]
    public void Address_ValidationAndEquality_ShouldEnforceInvariants()
    {
        var a1 = new Address("Street 1", "City A", "Country X");
        var a2 = new Address("street 1", "city a", "country x");
        a1.Should().Be(a2);

        Action actEmpty = () => new Address("", "City", "Country");
        actEmpty.Should().Throw<DomainException>();
    }
}
