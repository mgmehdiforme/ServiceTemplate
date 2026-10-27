using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Infrastructure.Persistence;
using ServiceTemplate.Infrastructure.Persistence.Entities;

namespace ServiceTemplate.Infrastructure.Ai;

/// <summary>
/// Service implementing vector embedding generation and co-located pgvector similarity search.
/// </summary>
public class SemanticSearchService : ISemanticSearchService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        ApplicationDbContext dbContext,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<SemanticSearchService> logger)
    {
        _dbContext = dbContext;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    public async Task IndexOrderAsync(Guid orderId, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        _logger.LogInformation("Generating vector embedding for OrderId: {OrderId}", orderId);

        var generated = await _embeddingGenerator.GenerateAsync([content], cancellationToken: cancellationToken);
        var floatArray = generated[0].Vector.ToArray();
        var vector = new Vector(floatArray);

        var existing = await _dbContext.OrderEmbeddings.FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (existing is not null)
        {
            existing.IndexedContent = content;
            existing.Embedding = vector;
            existing.GeneratedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            var newEmbedding = new OrderEmbedding
            {
                OrderId = orderId,
                IndexedContent = content,
                Embedding = vector,
                GeneratedAt = DateTimeOffset.UtcNow,
                ModelVersion = _embeddingGenerator.Metadata.ModelId ?? "default"
            };
            _dbContext.OrderEmbeddings.Add(newEmbedding);
        }
    }

    public async Task<IReadOnlyList<OrderSearchResult>> SearchOrdersAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<OrderSearchResult>();
        }

        var topCount = topK <= 0 ? 5 : topK;
        var generated = await _embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);
        var queryVector = new Vector(generated[0].Vector.ToArray());

        if (!_dbContext.Database.IsNpgsql())
        {
            var allEmbeddings = await _dbContext.OrderEmbeddings
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return allEmbeddings
                .Select(e =>
                {
                    var sim = CosineSimilarity(e.Embedding.ToArray(), queryVector.ToArray());
                    return new OrderSearchResult(e.OrderId, e.IndexedContent, Math.Round(sim, 4));
                })
                .OrderByDescending(r => r.SimilarityScore)
                .Take(topCount)
                .ToList();
        }

        var matches = await _dbContext.OrderEmbeddings
            .AsNoTracking()
            .OrderBy(e => e.Embedding.CosineDistance(queryVector))
            .Take(topCount)
            .Select(e => new
            {
                e.OrderId,
                e.IndexedContent,
                Distance = e.Embedding.CosineDistance(queryVector)
            })
            .ToListAsync(cancellationToken);

        return matches
            .Select(m => new OrderSearchResult(
                m.OrderId,
                m.IndexedContent,
                Math.Max(0.0, Math.Round(1.0 - m.Distance, 4))
            ))
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0.0;
        double dot = 0.0, magA = 0.0, magB = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        var denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom < 1e-9 ? 0.0 : dot / denom;
    }
}
