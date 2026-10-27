namespace ServiceTemplate.Application.Orders.SearchOrders;

/// <summary>
/// Combined response containing ranked pgvector order matches and AI synthesis.
/// </summary>
public sealed record SearchOrdersResponse(
    string Query,
    int TopK,
    IReadOnlyList<OrderSearchResultItem> Matches,
    string AiSynthesis
);

/// <summary>
/// An individual ranked match from the vector similarity search.
/// </summary>
public sealed record OrderSearchResultItem(
    Guid OrderId,
    string IndexedContent,
    double SimilarityScore
);
