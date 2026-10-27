using FluentAssertions;
using ServiceTemplate.Application.AiTools;
using Xunit;

namespace ServiceTemplate.UnitTests.Application;

public class AiPluginsTests
{
    [Fact]
    public async Task OrderInventoryPlugin_WithValidSku_ShouldReturnInventory()
    {
        var plugin = new OrderInventoryPlugin();
        var result = await plugin.CheckInventoryAsync("SKU-123");

        result.Should().Contain("SKU-123");
        result.Should().Contain("42 units in stock");
    }

    [Fact]
    public async Task OrderSummaryPlugin_WithValidOrderDetails_ShouldReturnSummary()
    {
        var plugin = new OrderSummaryPlugin();
        var result = await plugin.SummarizeOrderAsync("order-99", "cust-42", "299.99 USD");

        result.Should().Contain("order-99");
        result.Should().Contain("cust-42");
        result.Should().Contain("299.99 USD");
    }
}
