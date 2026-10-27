using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Orders.SearchOrders;
using Wolverine;

namespace ServiceTemplate.Api.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI");

        group.MapGet("/orders/search", async (
            [FromQuery] string q,
            [FromQuery] int? topK,
            [FromServices] IMessageBus bus,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Query string parameter 'q' is required."
                });
            }

            var query = new SearchOrdersQuery(q, topK ?? 5);
            var response = await bus.InvokeAsync<SearchOrdersResponse>(query, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("SearchOrdersSemantic")
        .WithSummary("Performs natural language vector search over orders using pgvector and AI synthesis")
        .Produces<SearchOrdersResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/orders/search/stream", async (
            [FromQuery] string q,
            [FromQuery] int? topK,
            [FromServices] ISemanticSearchService searchService,
            [FromServices] IChatClient chatClient,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Query parameter 'q' is required.", cancellationToken);
                return;
            }

            httpContext.Response.ContentType = "text/event-stream; charset=utf-8";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            var topCount = topK ?? 5;
            var searchResults = await searchService.SearchOrdersAsync(q, topCount, cancellationToken);

            var metaPayload = JsonSerializer.Serialize(new { type = "metadata", matchCount = searchResults.Count });
            await httpContext.Response.WriteAsync($"data: {metaPayload}\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);

            if (searchResults.Count == 0)
            {
                var noResultsPayload = JsonSerializer.Serialize(new { type = "token", content = $"No orders found matching query '{q}'." });
                await httpContext.Response.WriteAsync($"data: {noResultsPayload}\n\n", cancellationToken);
            }
            else
            {
                var prompt = $"You are an intelligent order assistant. Synthesize the customer orders found: {string.Join(" | ", searchResults.Select(m => m.IndexedContent))} for query: '{q}'. Provide a concise summary of the matched purchase intent.";
                var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

                await foreach (var update in chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        var tokenPayload = JsonSerializer.Serialize(new { type = "token", content = update.Text });
                        await httpContext.Response.WriteAsync($"data: {tokenPayload}\n\n", cancellationToken);
                        await httpContext.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            }

            await httpContext.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        })
        .WithName("SearchOrdersSemanticStream")
        .WithSummary("Streams real-time natural language synthesis tokens via Server-Sent Events (SSE)")
        .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

        return group;
    }
}
