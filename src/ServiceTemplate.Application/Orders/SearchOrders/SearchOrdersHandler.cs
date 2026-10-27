using Microsoft.Extensions.AI;
using ServiceTemplate.Application.Common.Interfaces;

namespace ServiceTemplate.Application.Orders.SearchOrders;

/// <summary>
/// Wolverine handler orchestrating semantic vector search and IChatClient synthesis.
/// </summary>
public class SearchOrdersHandler
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IChatClient _chatClient;

    public SearchOrdersHandler(ISemanticSearchService semanticSearchService, IChatClient chatClient)
    {
        _semanticSearchService = semanticSearchService;
        _chatClient = chatClient;
    }

    public async Task<SearchOrdersResponse> Handle(SearchOrdersQuery query, CancellationToken cancellationToken)
    {
        var topK = query.TopK <= 0 ? 5 : query.TopK;
        var searchResults = await _semanticSearchService.SearchOrdersAsync(query.Query, topK, cancellationToken);

        var matches = searchResults
            .Select(r => new OrderSearchResultItem(r.OrderId, r.IndexedContent, r.SimilarityScore))
            .ToList();

        string aiSynthesis;
        if (matches.Count == 0)
        {
            aiSynthesis = $"No orders found matching the query '{query.Query}'.";
        }
        else
        {
            var prompt = $"You are an intelligent order assistant. Synthesize the customer orders found: {string.Join(" | ", matches.Select(m => m.IndexedContent))} for query: '{query.Query}'. Provide a concise summary of the matched purchase intent.";
            var chatCompletion = await _chatClient.CompleteAsync(prompt, cancellationToken: cancellationToken);
            aiSynthesis = chatCompletion.Message.Text ?? "No synthesis generated.";
        }

        return new SearchOrdersResponse(query.Query, topK, matches, aiSynthesis);
    }
}
