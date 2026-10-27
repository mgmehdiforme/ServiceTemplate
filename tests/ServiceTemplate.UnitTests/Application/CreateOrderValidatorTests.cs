using FluentAssertions;
using ServiceTemplate.Application.Orders.CreateOrder;
using Xunit;

namespace ServiceTemplate.UnitTests.Application;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    private static CreateOrderCommand CreateValidCommand() => new(
        CustomerId: "cust_12345",
        Items:
        [
            new CreateOrderItemDto("PROD-1", "Mechanical Keyboard", 1, 149.99m),
            new CreateOrderItemDto("PROD-2", "Mousepad", 2, 29.99m)
        ],
        ShippingAddress: new ShippingAddressDto("10 Downing St", "London", "UK"),
        Currency: "USD"
    );

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyCustomerId_ShouldFail(string? customerId)
    {
        // Arrange
        var command = CreateValidCommand() with { CustomerId = customerId! };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.CustomerId));
    }

    [Fact]
    public async Task Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Items = [] };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.Items));
    }

    [Theory]
    [InlineData(0, 10.0)]
    [InlineData(-1, 10.0)]
    [InlineData(1, 0.0)]
    [InlineData(1, -5.0)]
    public async Task Validate_WithInvalidItemQuantityOrPrice_ShouldFail(int qty, decimal price)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto("SKU-1", "Test Product", qty, price)]
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "London", "UK")]
    [InlineData("Street", "", "UK")]
    [InlineData("Street", "London", "")]
    public async Task Validate_WithEmptyShippingFields_ShouldFail(string street, string city, string country)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            ShippingAddress = new ShippingAddressDto(street, city, country)
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
