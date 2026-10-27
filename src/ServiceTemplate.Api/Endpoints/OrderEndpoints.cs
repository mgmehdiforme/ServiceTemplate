using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Orders.CreateOrder;
using Wolverine;

namespace ServiceTemplate.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", async (
            [FromBody] CreateOrderCommand command,
            [FromServices] IMessageBus bus,
            CancellationToken cancellationToken) =>
        {
            var response = await bus.InvokeAsync<CreateOrderResponse>(command, cancellationToken);
            return Results.Created($"/api/orders/{response.Id}", response);
        })
        .WithName("CreateOrder")
        .WithSummary("Creates a new customer order enlisting in transactional outbox")
        .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var order = await dbContext.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Order Not Found",
                    Detail = $"Order with id '{id}' was not found."
                });
            }

            var dto = new OrderDetailsDto(
                order.Id,
                order.CustomerId,
                order.Status.ToString(),
                order.TotalAmount.Amount,
                order.TotalAmount.Currency,
                order.CreatedAt,
                order.UpdatedAt,
                new ShippingAddressDto(order.ShippingAddress.Street, order.ShippingAddress.City, order.ShippingAddress.Country),
                order.Items.Select(i => new OrderItemDetailsDto(
                    i.Id,
                    i.Sku,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice.Amount,
                    i.Total.Amount
                )).ToList()
            );

            return Results.Ok(dto);
        })
        .WithName("GetOrderById")
        .WithSummary("Retrieves an order by its unique identifier")
        .Produces<OrderDetailsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}

public sealed record OrderDetailsDto(
    Guid Id,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<OrderItemDetailsDto> Items
);

public sealed record OrderItemDetailsDto(
    Guid Id,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Total
);
