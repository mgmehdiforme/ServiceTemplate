using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

namespace ServiceTemplate.Infrastructure.Ai.Decorators;

/// <summary>
/// OpenTelemetry decorator conforming to GenAI semantic conventions.
/// </summary>
public sealed class TelemetryChatClient : IChatClient
{
    private static readonly ActivitySource ActivitySource = new("ServiceTemplate.Ai", "1.0.0");
    private readonly IChatClient _innerClient;

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public TelemetryChatClient(IChatClient innerClient)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("gen_ai.chat.completion", ActivityKind.Client);
        activity?.SetTag("gen_ai.system", Metadata.ProviderName ?? "generic");
        activity?.SetTag("gen_ai.request.model", Metadata.ModelId);

        try
        {
            var completion = await _innerClient.CompleteAsync(chatMessages, options, cancellationToken);
            activity?.SetTag("gen_ai.response.finish_reasons", completion.FinishReason?.ToString());
            return completion;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("error.message", ex.Message);
            activity?.AddTag("error.type", ex.GetType().FullName);
            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("gen_ai.chat.streaming", ActivityKind.Client);
        activity?.SetTag("gen_ai.system", Metadata.ProviderName ?? "generic");
        activity?.SetTag("gen_ai.request.model", Metadata.ModelId);

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
