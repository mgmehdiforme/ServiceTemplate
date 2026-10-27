using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;

namespace ServiceTemplate.Infrastructure.Ai.Decorators;

/// <summary>
/// Caching decorator leveraging HybridCache to eliminate redundant outbound model calls.
/// </summary>
public sealed class CachingChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly HybridCache _cache;
    private readonly TimeSpan _cacheDuration;

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public CachingChatClient(IChatClient innerClient, HybridCache cache, TimeSpan? cacheDuration = null)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = ComputeCacheKey(chatMessages);
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration
        };

        var cachedText = await _cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var completion = await _innerClient.CompleteAsync(chatMessages, options, token);
                return completion.Message.Text ?? string.Empty;
            },
            entryOptions,
            cancellationToken: cancellationToken
        );

        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, cachedText))
        {
            ModelId = Metadata.ModelId,
            FinishReason = ChatFinishReason.Stop
        };
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Streaming bypasses response cache to deliver instant incremental tokens
        await foreach (var update in _innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private static string ComputeCacheKey(IList<ChatMessage> messages)
    {
        var rawText = string.Join("\n", messages.Select(m => $"{m.Role}:{m.Text}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawText));
        return $"ai:chat:{Convert.ToHexString(hash)}";
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(IChatClient) ? this : _innerClient.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        _innerClient.Dispose();
    }
}
