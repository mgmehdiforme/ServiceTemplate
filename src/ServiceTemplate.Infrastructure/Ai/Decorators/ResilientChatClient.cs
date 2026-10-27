using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ServiceTemplate.Infrastructure.Ai.Decorators;

/// <summary>
/// Resilient decorator wrapping IChatClient with Polly v8 timeout, retry, and circuit breaker policies.
/// </summary>
public sealed class ResilientChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly ResiliencePipeline _pipeline;

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public ResilientChatClient(IChatClient innerClient, ResiliencePipeline? customPipeline = null)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _pipeline = customPipeline ?? CreateDefaultPipeline();
    }

    private static ResiliencePipeline CreateDefaultPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(15)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromMinutes(1),
                BreakDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>()
            })
            .Build();
    }

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async (state, token) => await _innerClient.CompleteAsync(state.chatMessages, state.options, token),
            (chatMessages, options),
            cancellationToken
        );
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
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
