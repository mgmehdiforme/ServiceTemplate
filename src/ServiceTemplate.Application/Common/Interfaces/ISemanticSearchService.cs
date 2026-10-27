namespace ServiceTemplate.Application.Common.Interfaces;

/// <summary>
/// Result item for semantic vector search queries.
/// </summary>
public sealed record OrderSearchResult(
    Guid OrderId,
    string IndexedContent,
    double SimilarityScore
);

/// <summary>
/// Abstraction for vector embedding generation and cosine similarity search over orders.
/// </summary>
public interface ISemanticSearchService
{
    Task IndexOrderAsync(Guid orderId, string content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSearchResult>> SearchOrdersAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}
