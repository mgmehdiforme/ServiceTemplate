using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace ServiceTemplate.Application.AiTools;

/// <summary>
/// Semantic Kernel tool plugin for querying stock and inventory levels.
/// </summary>
public sealed class OrderInventoryPlugin
{
    [KernelFunction, Description("Checks current inventory availability for a given product SKU.")]
    public Task<string> CheckInventoryAsync(
        [Description("The product SKU identifier")] string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        return Task.FromResult($"Product '{sku}' has 42 units in stock across local fulfillment hubs.");
    }
}
