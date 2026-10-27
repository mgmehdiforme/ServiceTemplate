using FluentAssertions;
using Microsoft.Extensions.AI;
using NSubstitute;
using ServiceTemplate.Infrastructure.Ai.Decorators;
using Xunit;

namespace ServiceTemplate.UnitTests.Ai;

public class AiDecoratorTests
{
    private readonly IChatClient _innerClient = Substitute.For<IChatClient>();

    public AiDecoratorTests()
    {
        _innerClient.Metadata.Returns(new ChatClientMetadata("MockProvider", new Uri("https://localhost"), "test-model"));
    }

    [Fact]
    public async Task ResilientChatClient_ShouldDelegateCompletionToInnerClient()
    {
        // Arrange
        var expectedCompletion = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Resilient response"));
        _innerClient.CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedCompletion));

        using var client = new ResilientChatClient(_innerClient);
        var messages = new List<ChatMessage> { new(ChatRole.User, "Test prompt") };

        // Act
        var result = await client.CompleteAsync(messages);

        // Assert
        result.Should().Be(expectedCompletion);
        await _innerClient.Received(1).CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TelemetryChatClient_ShouldDelegateCompletionAndPreserveMetadata()
    {
        // Arrange
        var expectedCompletion = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Telemetry response"));
        _innerClient.CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedCompletion));

        using var client = new TelemetryChatClient(_innerClient);
        client.Metadata.ModelId.Should().Be("test-model");

        var messages = new List<ChatMessage> { new(ChatRole.User, "Test prompt") };

        // Act
        var result = await client.CompleteAsync(messages);

        // Assert
        result.Should().Be(expectedCompletion);
        await _innerClient.Received(1).CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }
}
