using FluentAssertions;
using Microsoft.Extensions.AI;
using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Orders.SearchOrders;
using Xunit;

namespace ServiceTemplate.UnitTests.Application;

public class SearchOrdersHandlerTests
{
    private readonly ISemanticSearchService _semanticSearchService = Substitute.For<ISemanticSearchService>();
    private readonly IChatClient _chatClient = Substitute.For<IChatClient>();

    [Fact]
    public async Task Handle_WhenNoMatchesFound_ShouldReturnEmptyMatchesWithoutCallingChatModel()
    {
        // Arrange
        _semanticSearchService.SearchOrdersAsync("non-existent query", 5, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OrderSearchResult>>([]));

        var handler = new SearchOrdersHandler(_semanticSearchService, _chatClient);
        var query = new SearchOrdersQuery("non-existent query", 5);

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Matches.Should().BeEmpty();
        response.AiSynthesis.Should().Contain("No orders found matching");
        await _chatClient.DidNotReceiveWithAnyArgs().CompleteAsync(default(IList<ChatMessage>)!, default, default);
    }

    [Fact]
    public async Task Handle_WhenMatchesFound_ShouldInvokeChatClientAndReturnSynthesis()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var searchResults = new List<OrderSearchResult>
        {
            new(orderId, "Order for cust_1 containing 2x Mechanical Keyboards", 0.92)
        };

        _semanticSearchService.SearchOrdersAsync("keyboards", 5, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OrderSearchResult>>(searchResults));

        var simulatedChatCompletion = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Customer ordered 2 premium mechanical keyboards."));
        _chatClient.CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(simulatedChatCompletion));

        var handler = new SearchOrdersHandler(_semanticSearchService, _chatClient);
        var query = new SearchOrdersQuery("keyboards", 5);

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Matches.Should().HaveCount(1);
        response.Matches[0].OrderId.Should().Be(orderId);
        response.Matches[0].SimilarityScore.Should().Be(0.92);
        response.AiSynthesis.Should().Be("Customer ordered 2 premium mechanical keyboards.");
    }
}
