using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceTemplate.Api.Endpoints;
using ServiceTemplate.Application.Orders.CreateOrder;
using ServiceTemplate.IntegrationTests.Fixtures;
using Xunit;

namespace ServiceTemplate.IntegrationTests.Slices;

public class OrderEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidPayload_ShouldReturn201CreatedAndPersist()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: "cust_integration_1",
            Items:
            [
                new CreateOrderItemDto("SKU-INT-1", "Ergonomic Mechanical Keyboard", 2, 149.99m),
                new CreateOrderItemDto("SKU-INT-2", "Wrist Rest", 1, 29.99m)
            ],
            ShippingAddress: new ShippingAddressDto("10 Downing St", "London", "UK"),
            Currency: "USD"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        var b = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: b);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().NotBeEmpty();
        content.CustomerId.Should().Be("cust_integration_1");
        content.TotalAmount.Should().Be(329.97m);
        content.Status.Should().Be("Pending");

        // Verify retrieval via GET /api/orders/{id}
        var getResponse = await _client.GetAsync($"/api/orders/{content.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDetails = await getResponse.Content.ReadFromJsonAsync<OrderDetailsDto>();
        orderDetails.Should().NotBeNull();
        orderDetails!.Id.Should().Be(content.Id);
        orderDetails.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidPayload_ShouldReturn400BadRequestProblemDetails()
    {
        // Arrange: Missing CustomerId and empty items
        var invalidCommand = new CreateOrderCommand(
            CustomerId: "",
            Items: [],
            ShippingAddress: new ShippingAddressDto("", "", "")
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Errors.Should().ContainKey("CustomerId");
        problemDetails.Errors.Should().ContainKey("Items");
    }

    [Fact]
    public async Task GetOrder_WhenNotFound_ShouldReturn404NotFoundProblemDetails()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Order Not Found");
    }
}
