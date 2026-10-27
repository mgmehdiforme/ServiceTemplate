using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace ServiceTemplate.Application.AiTools;

/// <summary>
/// Semantic Kernel tool plugin for synthesizing order summaries for agentic chat workflows.
/// </summary>
public sealed class OrderSummaryPlugin
{
    [KernelFunction, Description("Summarizes order details for natural language chat assistants.")]
    public Task<string> SummarizeOrderAsync(
        [Description("The unique order identifier")] string orderId,
        [Description("Customer identifier")] string customerId,
        [Description("Total monetary amount formatted")] string totalAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(totalAmount);

        return Task.FromResult($"Order {orderId} for customer {customerId} has a verified total of {totalAmount}.");
    }
}
