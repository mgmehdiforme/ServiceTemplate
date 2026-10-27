using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Contracts.Events;
using ServiceTemplate.Domain.Exceptions;
using ServiceTemplate.Domain.Orders;

namespace ServiceTemplate.Application.Orders.ProcessPayment;

/// <summary>
/// Wolverine message handler consuming payment receipt events to transition orders to Paid and generate pgvector embeddings.
/// </summary>
public class OrderPaymentReceivedHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILogger<OrderPaymentReceivedHandler> _logger;

    public OrderPaymentReceivedHandler(
        IApplicationDbContext dbContext,
        ISemanticSearchService semanticSearchService,
        ILogger<OrderPaymentReceivedHandler> logger)
    {
        _dbContext = dbContext;
        _semanticSearchService = semanticSearchService;
        _logger = logger;
    }

    public async Task Handle(OrderPaymentReceived message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment received for OrderId: {OrderId}, Tx: {TransactionId}",
            message.OrderId, message.PaymentTransactionId);

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment processing.", message.OrderId);
            throw new OrderNotFoundException(message.OrderId);
        }

        // Idempotent safeguard: if already marked as Paid, acknowledge immediately
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} is already paid. Skipping duplicate processing.", order.Id);
            return;
        }

        order.MarkAsPaid();

        // Synthesize text for vector embedding
        var itemsSummary = string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.ProductName} ({i.Sku})"));
        var indexedContent = $"Order {order.Id} for customer {order.CustomerId}. Items: {itemsSummary}. Total: {order.TotalAmount}. Shipped to {order.ShippingAddress}.";

        await _semanticSearchService.IndexOrderAsync(order.Id, indexedContent, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully marked Order {OrderId} as Paid and queued vector embedding.", order.Id);
    }
}
