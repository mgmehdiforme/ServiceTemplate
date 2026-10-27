using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ServiceTemplate.Infrastructure.Ai.Mock;

/// <summary>
/// Deterministic offline chat client simulating structured reasoning and streaming for zero-key local execution.
/// </summary>
public sealed class OfflineMockChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; }

    public OfflineMockChatClient(string modelId = "offline-mock-chat")
    {
        Metadata = new ChatClientMetadata("OfflineMock", new Uri("https://localhost"), modelId);
    }

    public Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lastMessage = chatMessages.LastOrDefault()?.Text ?? string.Empty;
        var simulatedResponse = GenerateSimulatedResponse(lastMessage);

        var completion = new ChatCompletion(new ChatMessage(ChatRole.Assistant, simulatedResponse))
        {
            ModelId = Metadata.ModelId,
            FinishReason = ChatFinishReason.Stop
        };

        return Task.FromResult(completion);
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastMessage = chatMessages.LastOrDefault()?.Text ?? string.Empty;
        var simulatedResponse = GenerateSimulatedResponse(lastMessage);

        var tokens = simulatedResponse.Split(' ');
        for (int i = 0; i < tokens.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var token = (i == 0 ? tokens[i] : " " + tokens[i]);
            yield return new StreamingChatCompletionUpdate
            {
                Text = token,
                Role = ChatRole.Assistant,
                ModelId = Metadata.ModelId
            };

            await Task.Yield();
        }
    }

    private static string GenerateSimulatedResponse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "No content provided to synthesize.";
        }

        if (prompt.Contains("No orders found", StringComparison.OrdinalIgnoreCase))
        {
            return "No matching orders found for this query in the historical records.";
        }

        return "Synthesized order matches: identified relevant customer purchase records aligning with the natural language query.";
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(IChatClient) ? this : null;
    }

    public void Dispose()
    {
    }
}
