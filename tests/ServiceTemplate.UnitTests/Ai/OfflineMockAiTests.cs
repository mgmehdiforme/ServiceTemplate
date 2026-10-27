using FluentAssertions;
using Microsoft.Extensions.AI;
using ServiceTemplate.Infrastructure.Ai.Mock;
using Xunit;

namespace ServiceTemplate.UnitTests.Ai;

public class OfflineMockAiTests
{
    [Fact]
    public async Task GenerateAsync_ShouldProduceNormalized1536DimensionalVector()
    {
        // Arrange
        using var generator = new OfflineMockEmbeddingGenerator(1536, "offline-mock");
        var input = "High-end gaming workstation";

        // Act
        var result = await generator.GenerateAsync([input]);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var vector = result[0].Vector.ToArray();
        vector.Length.Should().Be(1536);

        // Verify L2 normalization (length ≈ 1.0)
        double sumSquares = vector.Sum(v => (double)v * v);
        var norm = Math.Sqrt(sumSquares);
        norm.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public async Task GenerateAsync_ShouldBeDeterministicForIdenticalInput()
    {
        // Arrange
        using var gen1 = new OfflineMockEmbeddingGenerator(1536);
        using var gen2 = new OfflineMockEmbeddingGenerator(1536);
        var input = "Ergonomic Mechanical Keyboard";

        // Act
        var res1 = await gen1.GenerateAsync([input]);
        var res2 = await gen2.GenerateAsync([input]);

        // Assert
        var vec1 = res1[0].Vector.ToArray();
        var vec2 = res2[0].Vector.ToArray();
        vec1.Should().Equal(vec2);
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnSimulatedResponseWithin50ms()
    {
        // Arrange
        using var client = new OfflineMockChatClient("offline-mock-chat");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Synthesize order search results for keyboards")
        };

        // Act
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var completion = await client.CompleteAsync(messages);
        watch.Stop();

        // Assert
        completion.Should().NotBeNull();
        completion.Message.Text.Should().NotBeNullOrWhiteSpace();
        completion.Message.Text.Should().Contain("Synthesized order matches");
        completion.FinishReason.Should().Be(ChatFinishReason.Stop);
        watch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task CompleteStreamingAsync_ShouldStreamTokenUpdates()
    {
        // Arrange
        using var client = new OfflineMockChatClient("offline-mock-chat");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Laptops in London")
        };

        // Act
        var tokens = new List<string>();
        await foreach (var update in client.CompleteStreamingAsync(messages))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                tokens.Add(update.Text);
            }
        }

        // Assert
        tokens.Should().NotBeEmpty();
        string.Concat(tokens).Should().Contain("Synthesized order matches");
    }
}
