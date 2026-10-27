namespace ServiceTemplate.Infrastructure.Ai.Configuration;

/// <summary>
/// Configuration options for AI model integrations and offline fallbacks.
/// </summary>
public class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Mock";
    public string ModelId { get; set; } = "gpt-4o-mini";
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public int VectorDimensions { get; set; } = 1536;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 15;
}
