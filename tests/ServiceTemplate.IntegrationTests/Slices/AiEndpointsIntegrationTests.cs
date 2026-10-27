using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ServiceTemplate.Application.Orders.SearchOrders;
using ServiceTemplate.IntegrationTests.Fixtures;
using Xunit;

namespace ServiceTemplate.IntegrationTests.Slices;

public class AiEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SearchOrders_WithValidQuery_ShouldReturn200AndSynthesis()
    {
        // Act
        var response = await _client.GetAsync("/api/ai/orders/search?q=ergonomic%20keyboards&topK=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SearchOrdersResponse>();
        content.Should().NotBeNull();
        content!.Query.Should().Be("ergonomic keyboards");
        content.TopK.Should().Be(3);
        content.AiSynthesis.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchOrders_WithMissingQuery_ShouldReturn400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/ai/orders/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchOrdersStream_ShouldReturnEventStreamAndCompleteWithDone()
    {
        // Act
        var response = await _client.GetAsync("/api/ai/orders/search/stream?q=laptops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var streamContent = await response.Content.ReadAsStringAsync();
        streamContent.Should().Contain("data: ");
        streamContent.Should().Contain("[DONE]");
    }
}
