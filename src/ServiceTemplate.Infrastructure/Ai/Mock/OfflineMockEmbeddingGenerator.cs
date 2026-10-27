using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace ServiceTemplate.Infrastructure.Ai.Mock;

/// <summary>
/// Deterministic offline embedding generator producing normalized float vectors for zero-dependency testing.
/// </summary>
public sealed class OfflineMockEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly int _dimensions;

    public EmbeddingGeneratorMetadata Metadata { get; }

    public OfflineMockEmbeddingGenerator(int dimensions = 1536, string modelId = "offline-mock-embeddings")
    {
        _dimensions = dimensions;
        Metadata = new EmbeddingGeneratorMetadata("OfflineMock", new Uri("https://localhost"), modelId, dimensions);
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = new GeneratedEmbeddings<Embedding<float>>();

        foreach (var value in values)
        {
            var vector = GenerateDeterministicVector(value ?? string.Empty, _dimensions);
            result.Add(new Embedding<float>(vector));
        }

        return Task.FromResult(result);
    }

    private static float[] GenerateDeterministicVector(string text, int dimensions)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var seed = BitConverter.ToInt32(hash, 0);
        var rng = new Random(seed);

        var vector = new float[dimensions];
        double sumSquares = 0.0;

        for (int i = 0; i < dimensions; i++)
        {
            // Sample standard normal distribution approximation using Box-Muller
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            vector[i] = (float)normal;
            sumSquares += normal * normal;
        }

        // L2 normalize
        var norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(IEmbeddingGenerator<string, Embedding<float>>) ? this : null;
    }

    public void Dispose()
    {
    }
}
