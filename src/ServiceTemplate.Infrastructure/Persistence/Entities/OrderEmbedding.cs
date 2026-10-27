namespace ServiceTemplate.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity storing the vector embedding representation of an order for similarity search.
/// </summary>
public class OrderEmbedding
{
    public Guid OrderId { get; set; }
    public string IndexedContent { get; set; } = string.Empty;
    public Pgvector.Vector Embedding { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ModelVersion { get; set; } = "text-embedding-3-small";
}
